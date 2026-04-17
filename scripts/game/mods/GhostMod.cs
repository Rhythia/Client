using System;
using Godot;

public class GhostMod : Mod, IObjectRenderModifier<Note>
{
    public override string Name => "Ghost";

    public override bool Rankable => true;

    public override double ScoreMultiplier => 1.03;

    public void ModifyRenderObject(Note note, float depth, Attempt attempt)
    {
        float ad = (float)attempt.Settings.ApproachDistance.Value;

        note.Transparency -= Mathf.Min(1, (ad - depth) / (ad / 2));
    }
}
