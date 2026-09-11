using System;
using System.Collections.Generic;
using Godot;

// Motorcycle mode's replacement for NoteRenderer: renders existing map Note
// objects as track gates/obstacles approaching the bike, instead of as
// flat notes on a grid. Reuses Note (X = lane, Millisecond = timing) rather
// than a new map object type, so map parsing/format is untouched.
public partial class GateRenderer : Renderer, IRenderer<Note>
{
    private MultiMeshInstance3D gateMesh { get; set; }

    public override void _Ready()
    {
        gateMesh = new()
        {
            Multimesh = new()
            {
                UseColors = true,
                Mesh = new BoxMesh()
            }
        };
        AddChild(gateMesh);
    }

    private bool doProcess(Note gate, float time, float approachTime)
    {
        return gate.Millisecond - time >= 0 && gate.Millisecond - time <= approachTime * 1000;
    }

    public void Render(double delta, double time, IList<Note> gates)
    {
        // TODO: lay gates out down the track (Z = distance until hit) instead
        // of on the flat X/Y plane NoteRenderer uses, once track art exists.
    }

    public override void Process(double delta, Attempt attempt)
    {
        if (!attempt.Objects.ContainsKey(typeof(Note)))
        {
            return;
        }

        _ = (List<Note>)attempt.Objects[typeof(Note)];
    }
}
