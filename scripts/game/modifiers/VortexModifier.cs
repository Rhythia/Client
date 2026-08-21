using System;
using Godot;

public class VortexModifier : Modifier, IObjectRenderModifier<Note>
{
    public override string Name => "Vortex";

    public override Color Color => new(0x9661ffff);

    public override bool Rankable => true;

    public override double ScoreMultiplier => 1.02;

    public void ModifyRenderObject(Note note, Attempt attempt)
    {
        float depth = Math.Max(0, -note.Transform.Origin.Z);

        if (depth > 0)
        {
            depth /= (float)attempt.Settings.ApproachDistance;

            float rotation = (float)Math.Pow(depth * 15, 1.5) / 60;
            float sine = (float)Math.Sin(attempt.Progress / attempt.Speed / 1000 * attempt.Settings.ApproachTime);
            rotation *= (float)(Math.Sign(sine) * Math.Pow(Math.Abs(sine), 0.75));

            note.Transform = note.Transform.Rotated(Vector3.Back, rotation);

            Vector3 offset = new(note.Transform.Origin.X, note.Transform.Origin.Y, 0);
            float pull = (float)Math.Pow(depth * 0.85, 3) * offset.DistanceTo(Vector3.Zero);

            note.Transform.Origin -= offset * pull;
        }
    }
}
