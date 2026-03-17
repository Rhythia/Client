using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Godot;
using Updatum;

public partial class Loading : BaseScene
{
    private Color opaque = new(1, 1, 1, 1);
    private Color transparent = new(1, 1, 1, 0);

    private ColorRect background;
    private TextureRect splash;
    private TextureRect splashShift;
    private Label progressLabel;
    private Panel progressBar;
    private Panel progressBarFill;

    public override async void _Ready()
    {
        base._Ready();


        background = GetNode<ColorRect>("Background");
        splash = GetNode<TextureRect>("Splash");
        splashShift = GetNode<TextureRect>("SplashShift");
        progressLabel = GetNode<Label>("ProgressLabel");
        progressBar = GetNode<Panel>("ProgressBar");
        progressBarFill = progressBar.GetNode<Panel>("Fill");

        progressLabel.Modulate = transparent;
        progressBar.Modulate = transparent;

        bool updateFound = false;
        try
        {
            updateFound = await Releases.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Could not get latest release: {ex.Message}");
        }

        if (updateFound)
        {
            var popup = new OptionPopup("Update Found", "Would you like to download the new version?");
            popup.AddOption("Update", Callable.From(updateStep));
            popup.AddOption("Cancel", Callable.From(mapInitializeStep));
            popup.Show();
        }
        else
        {
            mapInitializeStep();
        }
    }

    private void updateStep()
    {
        Tween inTween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetParallel();
        inTween.TweenProperty(background, "color", Color.FromHtml("#060509"), 1);
        inTween.TweenProperty(splash, "modulate", opaque, 0.5);
        inTween.TweenProperty(splashShift, "modulate", opaque, 0.25);
        inTween.TweenProperty(progressLabel, "modulate", opaque, 0.5);
        inTween.TweenProperty(progressBar, "modulate", opaque, 0.5);

        inTween.SetParallel(false);

        inTween.SetTrans(Tween.TransitionType.Sine);
        inTween.TweenProperty(splashShift, "modulate", transparent, 2.5);

        progressLabel.Text = $"Downloading {Releases.MANAGER.DownloadedPercentage} %";

        Releases.MANAGER.PropertyChanged += updateDownloadBar;
        Releases.MANAGER.DownloadCompleted += (_, _) =>
        {
            Releases.MANAGER.PropertyChanged -= updateDownloadBar;
            progressLabel.Text = "Installing";
        };
        Releases.UpdateToLatest();
    }

    private void mapInitializeStep()
    {
        int toSync = MapCache.FilesToSync.Value;
        bool allSynced = MapCache.FilesSynced.Value == toSync;

        if (allSynced)
        {
            progressLabel.Text = "Done!";
            progressBarFill.AnchorRight = 1;
        }
        else
        {
            MapCache.FilesSynced.ValueChanged += (_, _) =>
            {
                if (allSynced)
                {
                    return;
                }

                int synced = MapCache.FilesSynced.Value;
                float progress = synced / (float)toSync;

                progressLabel.Text = $"Initializing maps ({synced}/{toSync})";
                progressBarFill.AnchorRight = progress;

                if (progress >= 1)
                {
                    allSynced = true;
                    progressLabel.Text = "Done!";
                }
            };
        }

        Tween inTween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetParallel();
        inTween.TweenProperty(background, "color", Color.FromHtml("#060509"), 1);
        inTween.TweenProperty(splash, "modulate", opaque, 0.5);
        inTween.TweenProperty(splashShift, "modulate", opaque, 0.25);
        inTween.TweenProperty(progressLabel, "modulate", opaque, 0.5);
        inTween.TweenProperty(progressBar, "modulate", opaque, 0.5);

        inTween.SetParallel(false);

        inTween.SetTrans(Tween.TransitionType.Sine);
        inTween.TweenProperty(splashShift, "modulate", transparent, 2.5);

        inTween.TweenCallback(Callable.From(() =>
        {
            if (MapManager.Initialized)
            {
                exit();
            }
            else
            {
                MapManager.MapsInitialized += _ => Callable.From(exit).CallDeferred();
            }
        }));
    }

    private void updateDownloadBar(object _, PropertyChangedEventArgs @event)
    {
        if (@event.PropertyName == nameof(UpdatumManager.DownloadedPercentage))
        {
            CallDeferred("UpdateProgressLabel", $"Downloading {Releases.MANAGER.DownloadedPercentage} %");
            float progress = (float)Releases.MANAGER.DownloadedPercentage / 100;
            CallDeferred("UpdateProgressBar", progress);
        }
    }

    public void UpdateProgressBar(float progress)
    {
        progressBarFill.AnchorRight = progress;
    }

    public void UpdateProgressLabel(string label)
    {
        progressLabel.Text = label;
    }

    private void exit()
    {
        Tween outTween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetParallel();
        outTween.TweenProperty(background, "color", Color.Color8(0, 0, 0), 0.5);
        outTween.TweenProperty(splash, "modulate", Color.Color8(0, 0, 0), 0.5);
        outTween.Chain().TweenCallback(Callable.From(() => { SceneManager.Load("res://scenes/main_menu.tscn"); }));
    }
}
