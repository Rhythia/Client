using System;
using Godot;

// Motorcycle mode camera: follows behind/above the bike instead of locking
// to the cursor like CameraLock does for the grid mode.
public class CameraChase : CameraMode
{
    public override string Name => "Chase";

    public override bool Rankable => true;

    public override void Process(Attempt attempt, Camera3D camera, Vector2 mouseDelta)
    {
        var settings = attempt.Settings;

        float sensitivity = (float)settings.Sensitivity.Value;

        attempt.RawCursorPosition += new Vector2(1, 0) * mouseDelta / 120 * sensitivity;
        attempt.RawCursorPosition = new Vector2(
            Mathf.Clamp(attempt.RawCursorPosition.X, -Constants.MOTORCYCLE_ROAD_HALF_WIDTH, Constants.MOTORCYCLE_ROAD_HALF_WIDTH),
            0
        );
        attempt.CursorPosition = attempt.RawCursorPosition;
        attempt.BikeLaneOffset = attempt.CursorPosition.X;

        camera.Position = new Vector3(attempt.BikeLaneOffset, 1.5f, 3.75f);
        camera.Rotation = Vector3.Zero;

        attempt.CameraPosition = camera.Position;
        attempt.CameraRotation = camera.Rotation;
    }
}
