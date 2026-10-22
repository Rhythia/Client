using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using SQLite;
using Updatum;

public partial class Loading : BaseScene
{
    private Color opaque = new(1, 1, 1, 1);
    private Color transparent = new(1, 1, 1, 0);

    [Signal]
    private delegate void migrationEventHandler();

    private bool mapsDeleted = false;

    [Export]
    private ColorRect background;

    [Export]
    private TextureRect splash;

    [Export]
    private Label progressLabel;

    [Export]
    private Panel progressBar;

    [Export]
    private Panel progressBarFill;

    private ShaderMaterial splashMaterial;

    public override async void _Ready()
    {
        base._Ready();

        splashMaterial = splash.Material as ShaderMaterial;
        splashMaterial.SetShaderParameter("shift", 0);

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

            popup.Canceled += mapInitializeStep;

            popup.Show();
        }
        else
        {
            mapInitializeStep();
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

    private Tween enter()
    {
        var inTween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetParallel();
        inTween.TweenProperty(background, "color", Color.FromHtml("#060509"), 1);
        inTween.TweenProperty(progressLabel, "modulate", opaque, 0.5);
        inTween.TweenProperty(progressBar, "modulate", opaque, 0.5);
        inTween
            .SetTrans(Tween.TransitionType.Quint)
            .SetEase(Tween.EaseType.Out)
            .TweenMethod(
                Callable.From(
                    (float shift) =>
                    {
                        splashMaterial.SetShaderParameter("shift", shift);
                    }
                ),
                0.2,
                1.0,
                1.5
            );

        return inTween;
    }

    private void exit()
    {
        Action loadCallback = null;

        if (mapsDeleted)
        {
            loadCallback = () =>
            {
                _ = ToastNotification.Notify("Some of your maps were deleted due to them having no objects.", 2);
            };
            mapsDeleted = false;
        }

        var outTween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetParallel();
        outTween.TweenProperty(background, "color", Color.Color8(0, 0, 0), 0.25);
        outTween
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    SceneManager.Load("res://scenes/main_menu.tscn", callback: loadCallback);
                })
            );
    }

    private void updateStep()
    {
        enter();

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

        migration += migrateDbCallback;

        TableQuery<Map> maps = DatabaseService.Connection.Table<Map>();
        migrateDb(maps);
    }

    private void migrateDb(TableQuery<Map> maps)
    {
        if (DatabaseService.UserVersion >= 1)
        {
            CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.migration);
            return;
        }

        int offset = 0;
        int nMap = maps.Count();
        int chunkSize = Math.Min(nMap, Math.Clamp(nMap / 100 * 20, 50, 500));
        List<Map> mapsToDelete = [];

        float currentProgress = 0f;
        float progressStep = 1f / Math.Max(nMap, 1f);

        Task.Run(() =>
        {
            Callable
                .From(() =>
                {
                    progressLabel.Text = "Migrating DB";
                    progressBarFill.AnchorRight = 0f;
                })
                .CallDeferred();

            while (offset < nMap)
            {
                TableQuery<Map> chunk = maps.Skip(offset).Take(chunkSize);

                DatabaseService.Connection.RunInTransaction(() =>
                {
                    foreach (var map in chunk)
                    {
                        try
                        {
                            string objPath = Path.Combine(map.FolderPath, "objects.phxmo");
                            byte[] objectHash = SHA256.HashData(File.ReadAllBytes(objPath));
                            map.ObjectHash = Convert.ToHexStringLower(objectHash);
                        }
                        catch (FileNotFoundException)
                        {
                            mapsToDelete.Add(map);
                            continue;
                        }

                        DatabaseService.Connection.Update(map);
                    }
                });

                float step = progressStep * Math.Min(nMap - offset, chunkSize);
                currentProgress += step;
                Callable
                    .From(() =>
                    {
                        progressBarFill.AnchorRight = currentProgress;
                    })
                    .CallDeferred();

                offset += Math.Min(chunkSize, nMap - offset);
            }

            if (mapsToDelete.Count > 0)
            {
                mapsToDelete.ForEach(map => MapManager.Delete(map, true));
                mapsDeleted = true;
                Logger.Log(
                    $"The following maps were deleted for having no objects file: \n\t{string.Join(",\n\t", mapsToDelete.Select(item => item.Name))}"
                );
            }

            DatabaseService.UserVersion = 1;
            CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.migration);
        });
    }

    private void migrateDbCallback()
    {
        var inTween = enter();

        inTween.Chain();

        inTween.TweenCallback(
            Callable.From(() =>
            {
                if (MapManager.Initialized)
                {
                    exit();
                }
                else
                {
                    MapManager.MapsInitialized += _ => Callable.From(exit).CallDeferred();
                }
            })
        );
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
}
