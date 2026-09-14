using System;
using Godot;

public class ChaosModifier : Modifier, IObjectRenderModifier<Note>
{
    public override string Name => "Chaos";

    public override Color Color => new(0xff6198ff);

    public override bool Rankable => true;

    public override double ScoreMultiplier => 1.015;

    private FastNoiseLite noise = new();

    public void ModifyRenderObject(Note note, Attempt attempt)
    {
        float depth = Math.Max(0, -note.Transform.Origin.Z);

        if (depth > 0)
        {
            float n = noise.GetNoise3D(note.X * 2, note.Y * 2, note.Index + (float)(attempt.Progress / attempt.Speed / 400));
            var dir = Vector3.Up.Rotated(Vector3.Back, n * Mathf.Pi * 4);
            float offset = depth / (float)(double)attempt.Settings.ApproachDistance;
            offset = (float)Math.Pow(offset, 2);

            note.Transform.Origin += dir * offset;
        }
    }
}
