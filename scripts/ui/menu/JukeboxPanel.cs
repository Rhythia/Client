using System;
using Godot;

/// <summary>
/// Jukebox UI controls. The jukebox songs are handled in <see cref="SoundManager"/>
/// </summary>
public partial class JukeboxPanel : Panel, ISkinnable
{
    public static JukeboxPanel Instance;

    public Map Map;

    private Label title;
    private TextureButton pauseButton;
    private TextureButton skipButton;
    private TextureButton rewindButton;
    private Button selectButton;
    private AudioSpectrum spectrum;
    private ShaderMaterial spectrumMaterial;

    public override void _Ready()
    {
        Instance = this;

        title = GetNode<Label>("Title");
        pauseButton = GetNode<TextureButton>("Pause");
        skipButton = GetNode<TextureButton>("Skip");
        rewindButton = GetNode<TextureButton>("Rewind");
        selectButton = title.GetNode<Button>("GoTo");
        spectrum = GetNode<AudioSpectrum>("Spectrum");
        spectrumMaterial = spectrum.Material as ShaderMaterial;

        pauseButton.Pressed += pause;
        skipButton.Pressed += skip;
        rewindButton.Pressed += rewind;
        selectButton.Pressed += select;

        foreach (TextureButton button in new TextureButton[] { pauseButton, skipButton, rewindButton })
        {
            button.MouseEntered += () => { button.SelfModulate = Color.Color8(255, 255, 255); };
            button.MouseExited += () => { button.SelfModulate = Color.Color8(255, 255, 255, 190); };
        }

        selectButton.MouseEntered += () => { title.SelfModulate = Color.Color8(255, 255, 255); };
        selectButton.MouseExited += () => { title.SelfModulate = Color.Color8(255, 255, 255, 190); };

        if (SettingsManager.Instance.Settings.AutoplayJukebox)
        {
            pauseButton.TextureNormal = SkinManager.Instance.Skin.JukeboxPauseImage;
        }

        if (SoundManager.Map != null)
        {
            UpdateMap(SoundManager.Map);
        }

        UpdateSkin();

        SoundManager.Instance.JukeboxPlayed += UpdateMap;
        SoundManager.Instance.JukeboxEmpty += ClearMap;
        SkinManager.Instance.Loaded += UpdateSkin;
    }

    public override void _Process(double delta)
    {
        float progress = 0;

        if (SoundManager.Song.Stream != null)
        {
            progress = SoundManager.Song.GetPlaybackPosition() / (float)SoundManager.Song.Stream.GetLength();
        }

        spectrumMaterial.SetShaderParameter("progress", progress);
        spectrumMaterial.SetShaderParameter("margin", 1 - spectrum.Size.X / GetViewport().GetVisibleRect().Size.X);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed)
        {
            switch (eventKey.Keycode)
            {
                case Key.Mediaplay:
                    pause();
                    break;
                case Key.Medianext:
                    skip();
                    break;
                case Key.Mediaprevious:
                    rewind();
                    break;
            }
        }
    }

    public void ClearMap()
    {
        title.Text = "";
        Map = null;
    }

    public void UpdateMap(Map map)
    {
        Map = map;

        title.Text = map.PrettyTitle;

        pauseButton.TextureNormal = SkinManager.Instance.Skin.JukeboxPauseImage;
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        pauseButton.TextureNormal = SoundManager.Song.Playing ? skin.JukeboxPauseImage : skin.JukeboxPlayImage;
        skipButton.TextureNormal = skin.JukeboxSkipImage;
        rewindButton.TextureNormal = skin.JukeboxSkipImage;
    }

    private void pause()
    {
        var skin = SkinManager.Instance.Skin;
        SoundManager.Song.StreamPaused = !SoundManager.Song.StreamPaused;
        pauseButton.TextureNormal = SoundManager.Song.Playing ? skin.JukeboxPauseImage : skin.JukeboxPlayImage;
    }

    private void skip()
    {
        if (!SettingsManager.Instance.Settings.AutoplayJukebox.Value)
        {
            return;
        }

        SoundManager.JukeboxIndex++;
        SoundManager.PlayJukebox(SoundManager.JukeboxIndex);
    }

    private void rewind()
    {
        if (!SettingsManager.Instance.Settings.AutoplayJukebox.Value)
        {
            return;
        }

        if (SoundManager.Song.GetPlaybackPosition() < 2)
        {
            SoundManager.JukeboxIndex--;
            SoundManager.PlayJukebox(SoundManager.JukeboxIndex);
        }
        else
        {
            SoundManager.Song.Seek(0);
        }
    }

    private void select()
    {
        MapList.Instance.Select(Map, false);
    }
}
