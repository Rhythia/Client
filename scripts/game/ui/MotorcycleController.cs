using System;
using Godot;

// Motorcycle mode's replacement for Grid: instead of positioning a cursor on
// a 3x3 grid, it positions and leans the player's bike based on steering
// input recorded on the Attempt by a CameraMode (see CameraChase).
public partial class MotorcycleController : UIComponent
{
    private SettingsProfile settings;

    [Export]
    public Node3D Bike { get; set; }

    private float previousLaneOffset;

    public override void ApplySettings(SettingsProfile settings)
    {
        this.settings = settings;
    }

    public override void Process(double delta, Attempt state)
    {
        updateBikePosition(state.BikeLaneOffset);
        updateBikeLean(state.BikeLaneOffset, delta);
    }

    private void updateBikePosition(float laneOffset)
    {
        Bike.Position = new Vector3(laneOffset, 0, Bike.Position.Z);
    }

    // Lean the bike toward the direction it's steering, purely as visual feedback.
    private void updateBikeLean(float laneOffset, double delta)
    {
        float steeringVelocity = delta > 0 ? (laneOffset - previousLaneOffset) / (float)delta : 0f;
        previousLaneOffset = laneOffset;

        float targetLean = Mathf.Clamp(-steeringVelocity * 0.25f, -Mathf.Pi / 6, Mathf.Pi / 6);
        Bike.Rotation = new Vector3(Bike.Rotation.X, Bike.Rotation.Y, targetLean);
    }
}
