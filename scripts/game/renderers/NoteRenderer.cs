using System;
using System.Collections.Generic;
using Godot;

public partial class NoteRenderer : Renderer, IRenderer<Note>
{
    private MultiMeshInstance3D noteMesh { get; set; }

    private Color transparent = new Color(0x00000000);

    private Color white = new Color(0xffffffff);

    public override void _Ready()
    {
        noteMesh = new()
        {
            Multimesh = new()
            {
                UseColors = true,
                Mesh = new QuadMesh()
            }
        };
        AddChild(noteMesh);
    }

    private bool doProcess(Note note, float time, float approachTime)
    {
        return note.Millisecond - time >= 0 && note.Millisecond - time <= approachTime * 1000;
    }

    public void Render(double delta, double time, IList<Note> notes)
    {
        //float ar = (float)Settings.ApproachRate;
        //float ad = (float)Settings.ApproachDistance;
        //float at = (float)Settings.ApproachTime;
        //float noteSize = (float)Settings.NoteSize;
        //float fadeIn = (float)Settings.FadeIn;
        //bool pushback = Settings.Pushback;
        //var transform = new Transform3D(new Vector3(noteSize / 2, 0, 0), new Vector3(0, noteSize / 2, 0), new Vector3(0, 0, noteSize / 2), Vector3.Zero);

        //if (notes.Count > noteMesh.Multimesh.InstanceCount)
        //{
        //    noteMesh.Multimesh.InstanceCount = notes.Count;
        //}
        //else
        //{
        //    noteMesh.Multimesh.VisibleInstanceCount = notes.Count;
        //}

        //for (int i = 0; i < notes.Count; i++)
        //{
        //    var note = notes[i];

        //    if (!doProcess(note, (float)time, at) || note.Hit)
        //    {
        //        noteMesh.Multimesh.SetInstanceColor(i, transparent);
        //        continue;
        //    }

        //    // TODO: Change this to user note color
        //    var color = white;

        //    float depth = (note.Millisecond - (float)time) / (1000 * at) / ad / CurrentAttempt.Speed;
        //    note.Transparency = Math.Clamp((1 - depth / ad) / (fadeIn / 2), 0, 1);

        //    if (!pushback && note.Millisecond - attempt.Progress <= 0)
        //    {
        //        note.Transparency = 0;
        //    }
        //    else
        //    {
        //        if (Settings.FadeOut)
        //        {
        //            note.Transparency -= (ad - depth) / (ad + (float)Constants.HIT_WINDOW * ar / 1000);
        //        }

        //        foreach (Mod mod in attempt.Mods)
        //        {
        //            if (mod is IObjectRenderModifier<Note> modifier)
        //            {
        //                modifier.ModifyRenderObject(note, depth, attempt);
        //            }
        //        }
        //    }

        //    color.A = note.Transparency;

        //    transform.Origin = new Vector3(note.X, note.Y, -depth);

        //    noteMesh.Multimesh.SetInstanceTransform(i, transform);
        //    noteMesh.Multimesh.SetInstanceColor(i, color);
        //}
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
