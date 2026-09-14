using Godot;

public class GhostModifier : Modifier, IObjectRenderModifier<Note>
{
    public override string Name => "Ghost";

    public override Color Color => new(0xffffffff);

    public override bool Rankable => true;

    public override double ScoreMultiplier => 1.03;

    public override void Activate(Attempt attempt)
    {
        base.Activate(attempt);

        attempt.UseFadeOut = false;
    }

    public void ModifyRenderObject(Note note, Attempt attempt)
    {
        float ad = (float)(double)attempt.Settings.ApproachDistance;

        note.Opacity -= Mathf.Min(1, (ad + note.Transform.Origin.Z) / (ad / 2));
    }
}
