using Godot;
using System;

namespace Spaces;

public partial class Grid : BaseSpace
{
    private StandardMaterial3D tileMaterial;
    private WorldEnvironment environment;

    public override void _Ready()
    {
        base._Ready();

        tileMaterial = (GetNode<MeshInstance3D>("Top").Mesh as PlaneMesh).Material as StandardMaterial3D;
        environment = GetNode<WorldEnvironment>("WorldEnvironment");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        tileMaterial.AlbedoColor = NoteHitColor;
        tileMaterial.Uv1Offset += Vector3.Up * (float)delta * 3;
    }
}
