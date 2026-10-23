using Godot;

public class FlickerModifier : Modifier, IObjectRenderModifier<Note>
{
    public override string Name => "Flicker";

    public override Color Color => new(0xc5ff54ff);

    public override bool Rankable => true;

    public override double ScoreMultiplier => 1.035;

    public void ModifyRenderObject(Note note, Attempt attempt)
    {
        double at = attempt.Settings.ApproachTime;

        if (attempt.Progress / 600 / attempt.Speed % at >= at / 2)
        {
            if (note.Index % 2 == 0)
            {
                note.Opacity = 0;
            }
        }
        else
        {
            if (note.Index % 2 != 0)
            {
                note.Opacity = 0;
            }
        }
    }
}
