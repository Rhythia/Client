using System;
using Godot;

public class FlashlightModifier : Modifier, IModifier, IHudObjectModifier
{
    public override string Name => "Flashlight";

    public override Color Color => new(0xffffffff);

    public override bool Rankable => true;

    public override double ScoreMultiplier => 1.1;

    private float minFlashlightSize = 0.015f;

    private float maxFlashlightSize = 0.025f;

    private float minFlashlightSpinSize = 0.04f;

    private float maxFlashlightSpinSize = 0.055f;

    public float GetFlashlightSize(Attempt attempt)
    {
        bool cameraLock = attempt.CameraMode is CameraLock;
        long minFlashlightCombo = attempt.Map.Notes.Length / 100 * 25;

        return cameraLock
            ? maxFlashlightSize - ((maxFlashlightSize - minFlashlightSize) / minFlashlightCombo * Math.Min(attempt.Combo, minFlashlightCombo))
            : maxFlashlightSpinSize
                - ((maxFlashlightSpinSize - minFlashlightSpinSize) / minFlashlightCombo * Math.Min(attempt.Combo, minFlashlightCombo));
    }

    public void ModifyGrid(MeshInstance3D grid)
    {
        Node3D flashlight = grid.GetNode<Node3D>("Cursor/Flashlight");
        flashlight.Visible = true;
    }

    public void ResetGrid(MeshInstance3D grid)
    {
        Node3D flashlight = grid.GetNode<Node3D>("Cursor/Flashlight");
        flashlight.Visible = false;
    }
}
