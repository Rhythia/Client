using System.Collections;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using static LegacyRunner;

[GlobalClass]
public partial class GameComponent : Node3D
{
    [Export]
    public bool Standalone { get; set; } = true;

    [Export]
    public Camera3D Camera { get; set; }

    [Export]
    public Array<UIComponent> InterfaceComponents { get; set; }

    [Export]
    public Array<Renderer> Renderers { get; set; }

    [Signal]
    public delegate void AttemptProcessEventHandler(Attempt attempt);

    public HealthJudgment HealthProcessor { get; } = new HealthJudgment();

    public HitJudgment HitJudgement { get; } = new HitJudgment();

    public bool Playing { get; private set; } = true;

    public Attempt CurrentAttempt { get; private set; } = new();

    public void Play(Attempt attempt)
    {
        Input.MouseMode = CurrentAttempt.Settings.AbsoluteInput.Value ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Captured;
        Input.UseAccumulatedInput = false;

        HealthProcessor.ApplyAttempt(attempt);

        ApplySettings(attempt.Settings);
    }

    public override void _Ready()
    {
        ApplySettings(CurrentAttempt.Settings);

        // Automatically attempt to start the game if standalone
        if (Standalone)
        {
            // Play(MapManager.QueuedAttempt()]) -- MapManager will hold a Queue of attempts to play
            Play(CurrentAttempt);
        }
    }

    public override void _Process(double delta)
    {

        //  Psuedocode logic for the attempt
        //  
        //  bool[] hitResults = HitJudgment.ProcessHitJudgements(CurrentAttempt);
        //  foreach (var result in hitResults)
        //  {
        //      HealthJudgment.ApplyHitObjectResult(result);
        //      ScoreJudgment.ApplyHitObjectResult(result);
        //  }
        //
        //  CurrentAttempt.Health = HealthJudgment.Health;
        //  CurrentAttempt.Score = ScoreJudgment.Score;
        //
        //  if (HealthJudgment.IsFail) { Handle fail logic }

        // Update rendering (notes/objects) on attempt state
        foreach (var renderer in Renderers)
        {
            renderer.Process(delta, CurrentAttempt);
        }

        // Update interface components based on attempt state
        foreach (var component in InterfaceComponents)
        {
            component.Process(delta, CurrentAttempt);
        }

        EmitSignalAttemptProcess(CurrentAttempt);

    }

    public void ApplySettings(SettingsProfile settings)
    {
        CurrentAttempt.Settings = settings;

        foreach (var component in InterfaceComponents)
        {
            component.ApplySettings(settings);
        }

        foreach (var renderer in Renderers)
        {
            renderer.ApplySettings(settings);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Playing || !Standalone) { return; }

        if (@event is InputEventMouseMotion eventMouseMotion && Playing)
        {
            CurrentAttempt.CameraMode.Process(CurrentAttempt, Camera, eventMouseMotion.Relative);

            CurrentAttempt.DistanceMM += eventMouseMotion.Relative.Length() / CurrentAttempt.Settings.Sensitivity.Value / 57.5;
        }
    }
}
