using System;
using Godot;

// Motorcycle mode's replacement for Grid: instead of positioning a cursor on
// a 3x3 grid, this moves the bike between 3 discrete lanes using A/D input
// and (optionally) follows it with a chase camera. Runs every frame via
// Process(), unlike the old grid's mouse-driven CameraMode pipeline, since
// keyboard input isn't tied to mouse-motion events.
public partial class MotorcycleController : UIComponent
{
    private const float LaneChangeSpeed = 8f; // world units/second

    [Export]
    public Node3D Bike { get; set; }

    [Export]
    public Camera3D Camera { get; set; }

    private bool wasLeftPressed;
    private bool wasRightPressed;
    private float previousPositionX;

    public override void ApplySettings(SettingsProfile settings)
    {
    }

    public override void Process(double delta, Attempt state)
    {
        handleInput(state);
        updateBikePosition(state, delta);
        updateBikeLean(state, delta);
        updateCamera(state);
    }

    private void handleInput(Attempt state)
    {
        bool leftPressed = Input.IsPhysicalKeyPressed(Key.A);
        bool rightPressed = Input.IsPhysicalKeyPressed(Key.D);

        if (leftPressed && !wasLeftPressed)
        {
            state.BikeLane = Math.Max(state.BikeLane - 1, -1);
        }

        if (rightPressed && !wasRightPressed)
        {
            state.BikeLane = Math.Min(state.BikeLane + 1, 1);
        }

        wasLeftPressed = leftPressed;
        wasRightPressed = rightPressed;
    }

    private void updateBikePosition(Attempt state, double delta)
    {
        float targetX = MotorcycleLanes.LaneWorldX(state.BikeLane);
        state.BikeLaneOffset = Mathf.MoveToward(state.BikeLaneOffset, targetX, LaneChangeSpeed * (float)delta);

        Bike.Position = new Vector3(state.BikeLaneOffset, Bike.Position.Y, Bike.Position.Z);
    }

    // Lean the bike toward the direction it's steering, purely as visual feedback.
    private void updateBikeLean(Attempt state, double delta)
    {
        float velocity = delta > 0 ? (state.BikeLaneOffset - previousPositionX) / (float)delta : 0f;
        previousPositionX = state.BikeLaneOffset;

        state.BikeLean = Mathf.Clamp(-velocity * 0.15f, -Mathf.Pi / 6, Mathf.Pi / 6);
        Bike.Rotation = new Vector3(Bike.Rotation.X, Bike.Rotation.Y, state.BikeLean);
    }

    private void updateCamera(Attempt state)
    {
        if (Camera == null)
        {
            return;
        }

        Camera.Position = new Vector3(state.BikeLaneOffset, Camera.Position.Y, Camera.Position.Z);

        state.CameraPosition = Camera.Position;
        state.CameraRotation = Camera.Rotation;
    }
}
