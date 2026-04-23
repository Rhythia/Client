using System;
using System.IO;
using Godot;

public partial class MapInfoContainer : Panel, ISkinnable
{
    /// <summary>
    /// Parsed map reference
    /// </summary>
    public Map Map;

    public Leaderboard Leaderboard = new();

    private readonly PackedScene leaderboardScoreTemplate = ResourceLoader.Load<PackedScene>("res://prefabs/score_panel.tscn");

    private Panel info;
    private TextureRect coverBackground;
    private TextureRect cover;
    private Panel infoSubholder;
    private RichTextLabel mainLabel;
    private string mainLabelFormat;
    private RichTextLabel extraLabel;
    private string extraLabelFormat;
    private LinkPopupButton artistLink;
    private string artistLinkFormat;
    private HBoxContainer mapButtonsContainer;
    private Button favoriteButton;
    private Button videoButton;
    private FileDialog videoDialog;
    private Button copyButton;
    private FileDialog copyDialog;
    private Button deleteButton;

    private Panel actions;
    private Panel previewHolder;
    private Panel modesHolder;
    private Panel modifiersHolder;
    private Panel speedHolder;
    private HBoxContainer speedPresets;
    private Panel playHolder;
    private Button startButton;

    private Panel leaderboard;
    private ScrollContainer lbScrollContainer;
    private VBoxContainer lbContainer;
    private Button lbExpand;
    private Button lbHide;

    private ColorRect dim;
    private ShaderMaterial outlineMaterial;

    public override void _Ready()
    {
        info = GetNode<Panel>("Info");

        Panel infoHolder = info.GetNode("ScrollContainer").GetNode<Panel>("Holder");

        coverBackground = infoHolder.GetNode("CoverContainer").GetNode<TextureRect>("Background");
        cover = coverBackground.GetNode<TextureRect>("Cover");
        infoSubholder = infoHolder.GetNode<Panel>("Subholder");
        mainLabel = infoSubholder.GetNode<RichTextLabel>("MainLabel");
        mainLabelFormat = mainLabel.Text;
        extraLabel = infoSubholder.GetNode<RichTextLabel>("Extra");
        extraLabelFormat = extraLabel.Text;
        artistLink = coverBackground.GetNode<LinkPopupButton>("ArtistLink");
        artistLinkFormat = artistLink.Text;
        mapButtonsContainer = infoSubholder.GetNode<HBoxContainer>("Buttons");
        favoriteButton = mapButtonsContainer.GetNode<Button>("Favorite");
        videoButton = mapButtonsContainer.GetNode<Button>("Video");
        videoDialog = videoButton.GetNode<FileDialog>("VideoDialog");
        copyButton = mapButtonsContainer.GetNode<Button>("Copy");
        copyDialog = copyButton.GetNode<FileDialog>("CopyDialog");
        deleteButton = mapButtonsContainer.GetNode<Button>("Delete");

        favoriteButton.Pressed += () =>
        {
            Map.Favorite = !Map.Favorite;
            MapManager.Update(Map);

            var skin = SkinManager.Instance.Skin;

            favoriteButton.TooltipText = Map.Favorite ? "Unfavorite" : "Favorite";
            favoriteButton.Icon = Map.Favorite ? skin.UnfavoriteButtonImage : skin.FavoriteButtonImage;
        };

        //videoButton.Pressed += () =>
        //{
        //    videoDialog.Popup();
        //};

        //videoDialog.FileSelected += (file) =>
        //{
        //    MapManager.InsertVideo(Map, file);
        //};

        copyButton.Pressed += () =>
        {
            copyDialog.Popup();

        };

        copyDialog.FileSelected += (path) =>
        {
            File.Copy(Map.FilePath, path);
            _ = ToastNotification.Notify($"Copied to {path}");
        };

        deleteButton.Pressed += () =>
        {
            MapManager.Delete(Map);
        };

        void updateOffset() { infoSubholder.OffsetLeft = coverBackground.Size.X + 8; }

        coverBackground.Connect("resized", Callable.From(updateOffset));

        actions = GetNode<Panel>("Actions");

        Panel actionsHolder = actions.GetNode("ScrollContainer").GetNode<Panel>("Holder");

        previewHolder = actionsHolder.GetNode<Panel>("Preview");
        modesHolder = actionsHolder.GetNode<Panel>("Modes");
        modifiersHolder = actionsHolder.GetNode<Panel>("Modifiers");
        speedHolder = actionsHolder.GetNode<Panel>("Speed");
        speedPresets = speedHolder.GetNode("ScrollContainer").GetNode<HBoxContainer>("Presets");
        playHolder = actionsHolder.GetNode<Panel>("Play");
        startButton = playHolder.GetNode<Button>("Button");

        leaderboard = GetNode<Panel>("Leaderboard");
        lbScrollContainer = leaderboard.GetNode<ScrollContainer>("ScrollContainer");
        lbContainer = lbScrollContainer.GetNode<VBoxContainer>("VBoxContainer");
        lbExpand = leaderboard.GetNode<Button>("Expand");
        lbHide = GetNode<Button>("LeaderboardHide");

        dim = GetNode<ColorRect>("Dim");
        outlineMaterial = info.GetNode<Panel>("Outline").Material as ShaderMaterial;

        Modulate = Color.Color8(255, 255, 255, 0);

        // Speed setup

        HSlider speedSlider = speedHolder.GetNode<HSlider>("HSlider");
        LineEdit speedEdit = speedHolder.GetNode<LineEdit>("LineEdit");

        void displaySpeed(double speed)
        {
            if (!IsInstanceValid(speedSlider)) { return; }

            speed *= 100;

            double rounded = Math.Round(speed * 10) / 10;

            if (Math.Abs(rounded - speed) <= Mathf.Epsilon)
            {
                speed = rounded;
            }

            speedSlider.SetValueNoSignal(speed);
            speedEdit.Text = speed.ToString();
        }

        displaySpeed(Lobby.Speed);

        Lobby.Instance.SpeedChanged += displaySpeed;

        void applySpeed()
        {
            if (!double.TryParse(speedEdit.Text, System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                value = 100;
            }

            value = Math.Clamp(value, 25, 1000) / 100;

            Lobby.SetSpeed(value);

            if (SoundManager.Map?.Name == Map.Name && SoundManager.Song.Playing)
            {
                SoundManager.Song.PitchScale = (float)Lobby.Speed;
            }
        }

        speedEdit.FocusExited += applySpeed;
        speedEdit.TextSubmitted += (_) => { applySpeed(); };
        speedSlider.ValueChanged += (value) =>
        {
            speedEdit.Text = value.ToString();
            applySpeed();
        };

        // StartFrom setup

        HSlider startFromSlider = playHolder.GetNode<HSlider>("HSlider");
        LineEdit startFromEdit = playHolder.GetNode<LineEdit>("LineEdit");

        void displayStartFrom(double startFrom)
        {
            if (!IsInstanceValid(startFromSlider)) { return; }

            int length = Map != null ? Map.Length : 1;

            startFromSlider.SetValueNoSignal(startFrom / length);
            startFromEdit.Text = Util.String.FormatTime(startFrom / 1000);
            startButton.Text = $"START{(startFrom > 0 ? $" ({startFromEdit.Text})" : "")}";
        }

        Lobby.Instance.StartFromChanged += displayStartFrom;

        void applyStartFrom(string input = null, bool seek = true)
        {
            input ??= startFromEdit.Text == "" ? startFromEdit.PlaceholderText : startFromEdit.Text;

            double value = 0;
            string[] split = input.Split(":");

            split.Reverse();

            if (split.Length > 1 && split[1].IsValidFloat())
            {
                value += 60 * split[1].ToFloat();
            }

            if (double.TryParse(split[0], System.Globalization.CultureInfo.InvariantCulture, out double inputValue))
            {
                if (inputValue < 1)
                {
                    inputValue *= Map.Length / 1000;
                }

                value += inputValue;
            }

            value = Math.Clamp(value * 1000, 0, Map.Length);

            Lobby.SetStartFrom(value);

            if (SoundManager.Map?.Name != Map.Name)
            {
                SoundManager.StartMapSelectionPlayback(Map);
            }

            if (seek && SoundManager.Song.Playing)
            {
                SoundManager.Song.Seek((float)Lobby.StartFrom / 1000);
            }
        }

        startFromEdit.FocusExited += () => { applyStartFrom(); };
        startFromEdit.TextSubmitted += (_) => { applyStartFrom(); };
        startFromSlider.ValueChanged += value =>
        {
            applyStartFrom((Math.Round(value * Map.Length) / 1000).ToString("F2", new System.Globalization.CultureInfo("en-US")), false);
        };
        startFromSlider.DragEnded += changed =>
        {
            if (changed) { applyStartFrom(); }
        };

        //

        startButton.Pressed += () =>
        {
            LegacyRunner.Play(Map, Lobby.Speed, Lobby.StartFrom, Lobby.Modifiers);
        };

        // Leaderboard

        Panel lbExpandHover = lbExpand.GetNode<Panel>("Hover");

        void tweenExpandHover(bool show)
        {
            CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quart).TweenProperty(lbExpandHover, "modulate", Color.Color8(255, 255, 255, (byte)(show ? 255 : 0)), 0.25);
        }

        lbExpand.MouseEntered += () => { tweenExpandHover(true); };
        lbExpand.MouseExited += () => { tweenExpandHover(false); };
        lbExpand.Pressed += () => { toggleLeaderboard(true); };
        lbHide.Pressed += () => { toggleLeaderboard(false); };

        //

        SkinManager.Instance.Loaded += UpdateSkin;

        UpdateSkin();
    }

    public override void _Process(double delta)
    {
        outlineMaterial?.SetShaderParameter("cursor_position", GetViewport().GetMousePosition());
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.Right: toggleLeaderboard(false); break;
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.Left: toggleLeaderboard(false); break;
            }
        }
    }

    public void Setup(Map map)
    {
        if (map == null) return;

        if (Name == map.Name)
        {
            return;
        }

        Map = map;
        Name = map.Name;

        // Transition

        Position = Vector2.Zero;
        Scale = Vector2.One;

        info.OffsetLeft -= 64;
        info.OffsetRight -= 64;
        actions.OffsetLeft -= 80;
        actions.OffsetRight -= 80;
        leaderboard.OffsetLeft -= 96;
        leaderboard.OffsetRight -= 96;

        Tween inTween = CreateTween().SetEase(Tween.EaseType.Out).SetParallel();
        inTween.SetTrans(Tween.TransitionType.Quint).TweenProperty(info, "offset_left", 0, 0.5);
        inTween.TweenProperty(info, "offset_right", 0, 0.5);
        inTween.SetTrans(Tween.TransitionType.Quart).TweenProperty(actions, "offset_left", 0, 0.6);
        inTween.TweenProperty(actions, "offset_right", 0, 0.6);
        inTween.SetTrans(Tween.TransitionType.Cubic).TweenProperty(leaderboard, "offset_left", 0, 0.7);
        inTween.TweenProperty(leaderboard, "offset_right", 0, 0.7);

        OffsetRight = 0;
        Position += Vector2.Left * 64;

        // Info

        int clampedDifficulty = Math.Clamp(map.Difficulty, 0, Constants.DIFFICULTY_COLORS.Length - 1);
        mainLabel.Text = string.Format(mainLabelFormat, map.PrettyTitle, Constants.DIFFICULTY_COLORS[clampedDifficulty].ToHtml(), map.DifficultyName, map.PrettyMappers);
        extraLabel.Text = string.Format(extraLabelFormat, Util.String.FormatTime(map.Length / 1000), map.Notes.Length, map.Name);
        coverBackground.SelfModulate = Constants.DIFFICULTY_COLORS[clampedDifficulty];
        cover.Texture = map.Cover;
        favoriteButton.TooltipText = map.Favorite ? "Unfavorite" : "Favorite";
        favoriteButton.Icon = map.Favorite ? SkinManager.Instance.Skin.UnfavoriteButtonImage : SkinManager.Instance.Skin.FavoriteButtonImage;

        artistLink.Visible = map.ArtistLink != "";
        artistLink.Text = string.Format(artistLinkFormat, map.ArtistPlatform);

        artistLink.UpdateLink(map.ArtistLink);

        // Actions

        Lobby.SetStartFrom(0);

        previewHolder.GetNode("AspectRatioContainer").GetNode<FlatPreview>("FlatPreview").Setup(map, true);

        // Leaderboard

        Leaderboard = new();

        if (File.Exists($"{Constants.USER_FOLDER}/pbs/{map.Name}"))
        {
            Leaderboard = new(map.Name, $"{Constants.USER_FOLDER}/pbs/{map.Name}");
        }

        leaderboard.Visible = Leaderboard.Valid && Leaderboard.ScoreCount > 0;

        foreach (Node child in lbContainer.GetChildren())
        {
            lbContainer.RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < Math.Min(8, Leaderboard.ScoreCount); i++)
        {
            ScorePanel panel = leaderboardScoreTemplate.Instantiate<ScorePanel>();

            lbContainer.AddChild(panel);
            panel.Setup(Leaderboard.Scores[i]);
            panel.GetNode<ColorRect>("Background").Color = Color.Color8(255, 255, 255, (byte)(i % 2 * 8));

            panel.Button.Pressed += () => { toggleLeaderboard(false); };
        }
    }

    public void Refresh()
    {
        Setup(Map);
    }

    public Tween Transition(bool show)
    {
        Tween tween = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic).SetParallel();
        float time = show ? 0.4f : 0.3f;

        PivotOffset = Size / 2;

        tween.TweenProperty(this, "modulate", Color.Color8(255, 255, 255, (byte)(show ? 255 : 0)), time);
        tween.TweenProperty(this, "position", show ? Vector2.Zero : Vector2.Down * 24, time);
        tween.TweenProperty(this, "scale", Vector2.One * (show ? 1f : 0.9f), time);
        tween.Chain();

        return tween;
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        coverBackground.Texture = skin.MapInfoCoverBackgroundImage;
        speedPresets.GetNode("MinusMinus").GetNode<SpeedPresetButton>("SpeedPresetButton").Icon = skin.SpeedPresetMinusMinusButtonImage;
        speedPresets.GetNode("Minus").GetNode<SpeedPresetButton>("SpeedPresetButton").Icon = skin.SpeedPresetMinusButtonImage;
        speedPresets.GetNode("Middle").GetNode<SpeedPresetButton>("SpeedPresetButton").Icon = skin.SpeedPresetMiddleButtonImage;
        speedPresets.GetNode("Plus").GetNode<SpeedPresetButton>("SpeedPresetButton").Icon = skin.SpeedPresetPlusButtonImage;
        speedPresets.GetNode("PlusPlus").GetNode<SpeedPresetButton>("SpeedPresetButton").Icon = skin.SpeedPresetPlusPlusButtonImage;
    }

    private void toggleLeaderboard(bool show)
    {
        lbExpand.Visible = !show;
        lbHide.Visible = show;
        lbScrollContainer.VerticalScrollMode = show ? ScrollContainer.ScrollMode.Auto : ScrollContainer.ScrollMode.ShowNever;

        foreach (ScorePanel panel in lbContainer.GetChildren())
        {
            panel.Button.Visible = show;
        }

        Tween tween = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quart).SetParallel();

        tween.TweenProperty(leaderboard, "offset_top", -100 * (show ? Math.Min(4, Leaderboard.ScoreCount) : 1), 0.25);
        tween.TweenProperty(dim, "color", Color.Color8(0, 0, 0, (byte)(show ? 128 : 0)), 0.25);

        if (!show)
        {
            tween.TweenProperty(lbScrollContainer, "scroll_vertical", 0, 0.15);
        }
    }
}
