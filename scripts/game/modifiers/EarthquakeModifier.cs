using System;
using Godot;

public class EarthquakeModifier : Modifier, IObjectRenderModifier<Note>
{
    public override string Name => "Earthquake";

    public override Color Color => new(0xff4400ff);

    public override bool Rankable => true;

    public override double ScoreMultiplier => 1.01;

    private Random random = new();

    public void ModifyRenderObject(Note note, Attempt attempt)
    {
        float depth = Math.Max(0, -note.Transform.Origin.Z);

        if (depth > 0)
        {
            float rad = random.Next(-180, 180) * (Mathf.Pi / 180);
            var dir = Vector3.Up.Rotated(Vector3.Back, rad);
            float offset = depth / (float)(double)attempt.Settings.ApproachDistance;
            offset = (float)Math.Pow(offset, 1.5) / 2;

            note.Transform.Origin += dir * offset;
        }
    }
}
