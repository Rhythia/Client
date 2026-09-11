using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// Motorcycle mode's replacement for NoteRenderer: renders existing map Note
// objects as track gates approaching the bike, and resolves them via
// MotorcycleHitJudgment. Reuses Note (X = lane, Millisecond = timing)
// instead of a new map object type, so map parsing/format is untouched -
// GameComponent.Play() is what loads a Map's notes into Attempt.Objects.
public partial class GateRenderer : Renderer, IRenderer<Note>
{
    // How far ahead of the bike (world units / milliseconds) gates become visible.
    private const float ApproachDistance = 12f;

    private const float ApproachTimeMs = 1500f;

    private MultiMeshInstance3D gateMesh { get; set; }

    private readonly MotorcycleHitJudgment hitJudgment = new();

    private Color transparent = new Color(0x00000000);

    private Color white = new Color(0xffffffff);

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

    public void Render(double delta, double time, IList<Note> gates)
    {
        if (gates.Count > gateMesh.Multimesh.InstanceCount)
        {
            gateMesh.Multimesh.InstanceCount = gates.Count;
        }

        for (int i = 0; i < gates.Count; i++)
        {
            Note gate = gates[i];
            double msUntilHit = gate.Millisecond - time;

            if (gate.Hit || msUntilHit < -Constants.HIT_WINDOW || msUntilHit > ApproachTimeMs)
            {
                gateMesh.Multimesh.SetInstanceColor(i, transparent);
                continue;
            }

            int lane = MotorcycleLanes.LaneFromNoteX(gate.X);
            float laneX = MotorcycleLanes.LaneWorldX(lane);
            float z = -(float)(msUntilHit / ApproachTimeMs) * ApproachDistance;

            gateMesh.Multimesh.SetInstanceTransform(i, new Transform3D(Basis.Identity, new Vector3(laneX, 0, z)));
            gateMesh.Multimesh.SetInstanceColor(i, white);
        }
    }

    public override void Process(double delta, Attempt attempt)
    {
        if (!attempt.Objects.TryGetValue(typeof(Note), out IList<object> objects))
        {
            return;
        }

        List<Note> gates = objects.Cast<Note>().ToList();

        hitJudgment.ProcessGates(attempt, gates);
        Render(delta, attempt.Progress, gates);
    }
}
