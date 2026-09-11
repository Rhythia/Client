using System;
using Godot;

public class CameraSpin : CameraMode
{
    public override string Name => "Spin";

    public override bool Rankable => true;

    public override void Process(Attempt attempt, ReplayManager replayManager, Camera3D camera, MeshInstance3D cursor, Vector2 mouseDelta, float sensitivity)
    {
        var settings = attempt.Settings;

        if (!attempt.IsReplay)
        {
            camera.Rotation += new Vector3(-mouseDelta.Y / 120 * sensitivity / (float)Math.PI, -mouseDelta.X / 120 * sensitivity / (float)Math.PI, 0);
        }
        else
        {
            camera.Rotation += new Vector3(mouseDelta.Y / (float)Math.PI, -mouseDelta.X / (float)Math.PI, 0);
        }

        camera.Rotation = new Vector3((float)Math.Clamp(camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)), camera.Rotation.Y, camera.Rotation.Z);

        var origin = new Vector3(0, 0, 3.5f);
        var cursorLock = new Vector3(attempt.CursorPosition.X, attempt.CursorPosition.Y, 0);

        // The pivot is to mimic ROBLOX's orbital camera
        var pivot = camera.Basis.Z / 4f;

        // Proper Parallax Support
        camera.Position = origin + cursorLock * (float)(double)settings.CameraParallax + pivot;

        var lookVector = camera.Basis.Z;
        var cameraVector2 = new Vector2(camera.Position.X, camera.Position.Y);
        var lookVector2 = new Vector2(lookVector.X, lookVector.Y);

        // Project Cursor from Camera's "ray cast"
        attempt.RawCursorPosition = cameraVector2 - lookVector2 * Mathf.Abs(camera.Position.Z / lookVector.Z);
        attempt.CursorPosition = attempt.RawCursorPosition.Clamp(-Constants.BOUNDS, Constants.BOUNDS);

        Vector3 cursorPos = new(attempt.CursorPosition.X, attempt.CursorPosition.Y, 0);

        if (cursorPos.IsFinite())
        {
            cursor.Position = cursorPos;
        }
    }
}
