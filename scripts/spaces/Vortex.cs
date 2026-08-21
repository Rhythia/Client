using System;
using Godot;

namespace Spaces;

public partial class Vortex : BaseSpace
{
    private SettingsProfile settings;
    private SkinProfile skin;

    private Node3D squircles;
    private StandardMaterial3D[] squircleMaterials;
    private Color[] lastHitColors;
    private Color squircleColorReset = new("ffffff");

    public override void _Ready()
    {
        base._Ready();

        settings = SettingsManager.Instance.Settings;
        skin = SkinManager.Instance.Skin;

        squircles = GetNode<Node3D>("Squircles");

        int squircleCount = squircles.GetChildCount();

        squircleMaterials = new StandardMaterial3D[squircleCount];
        lastHitColors = new Color[squircleCount];

        // Start as if previous notes had been hit
        var noteColors = (Color[])skin.NoteColors.Clone();
        noteColors.Reverse();

        for (int i = 0; i < squircleCount; i++)
        {
            var squircle = squircles.GetChild<MeshInstance3D>(i);
            var mesh = squircle.Mesh.Duplicate() as QuadMesh;
            var material = mesh.Material.Duplicate() as StandardMaterial3D;

            mesh.Material = material;
            squircle.Mesh = mesh;
            squircleMaterials[i] = material;
            lastHitColors[i] = noteColors[i % noteColors.Length];
        }

        // Wait for new runner to implement Hit signal connection here
        // Update and push back lastHitColors order
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // nasty temporary
        if (Game.Instance.Runner.Attempt.LastHitColour != lastHitColors[0])
        {
            for (int i = lastHitColors.Length - 1; i > 0; i--)
            {
                lastHitColors[i] = lastHitColors[i - 1];
            }

            lastHitColors[0] = Game.Instance.Runner.Attempt.LastHitColour;
        }
        //

        // Rotation
        if (settings.SpaceEffects)
        {
            squircles.Rotation = Vector3.Forward * (Time.GetTicksMsec() / 8000f);
        }

        // Hit FX
        for (int i = 0; i < squircleMaterials.Length; i++)
        {
            var color = squircleColorReset;

            if (settings.SpaceHitEffects)
            {
                color = squircleMaterials[i].AlbedoColor.Lerp(lastHitColors[i], Math.Min(1, (float)delta * 6));
            }

            squircleMaterials[i].AlbedoColor = color;
        }
    }
}
