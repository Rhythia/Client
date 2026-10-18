using Godot;

public class AutoplayModifier : Modifier
{
    public override string Name => "Autoplay";

    public override Color Color => new(0xffd166ff);

    public override void Activate(Attempt attempt)
    {
        base.Activate(attempt);
        attempt.Qualifies = false;
    }
}
