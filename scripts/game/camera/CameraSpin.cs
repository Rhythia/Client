using System;
using Godot;

public class CameraSpin : CameraMode
{
    public override string Name => "Spin";

    public override bool Rankable => true;

    public override void Process(Attempt attempt, Camera3D camera, Vector2 mouseDelta)
    {
        var settings = attempt.Settings;

        float sensitivity = settings.Sensitivity.Value;
        sensitivity *= settings.FoV.Value / 70f;

        camera.Rotation += new Vector3(-mouseDelta.Y / 120 * sensitivity / (float)Math.PI, -mouseDelta.X / 120 * sensitivity / (float)Math.PI, 0);
        camera.Rotation = new Vector3(Math.Clamp(camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)), camera.Rotation.Y, camera.Rotation.Z);
        camera.Position = new Vector3(attempt.CursorPosition.X * 0.25f, attempt.CursorPosition.Y * 0.25f, 3.5f) + camera.Basis.Z / 4;

        attempt.CameraPosition = camera.Position;
        attempt.CameraRotation = camera.Rotation;

        float wtf = 0.95f;
        float hypotenuse = (wtf + attempt.CameraPosition.Z) / camera.Basis.Z.Z;
        float distance = (float)Math.Sqrt(Math.Pow(hypotenuse, 2) - Math.Pow(wtf + camera.Position.Z, 2));

        attempt.RawCursorPosition = new Vector2(camera.Basis.Z.X, camera.Basis.Z.Y).Normalized() * -distance;
        attempt.CursorPosition = attempt.RawCursorPosition.Clamp(-Constants.BOUNDS, Constants.BOUNDS);
    }
}
