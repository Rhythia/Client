using Godot;

public class HardrockModifier : Modifier, IHudObjectModifier, IMapModifier
{
    public override string Name => "Hardrock";

    public override Color Color => new(0x8000ffff);

    public override bool Rankable => true;

    public override double ScoreMultiplier => 1.08;

    public void ModifyGrid(MeshInstance3D grid)
    {
        if (SettingsManager.Instance.Settings.ExpandHudOnHr)
        {
            QuadMesh mesh = (QuadMesh)grid.Mesh;
            mesh.Size = new Vector2(mesh.Size.X * 1.35f, mesh.Size.Y * 1.35f);
        }
    }

    public void ModifyGridGuides(MeshInstance3D gridGuides)
    {
        if (SettingsManager.Instance.Settings.ExpandHudOnHr)
        {
            QuadMesh mesh = (QuadMesh)gridGuides.Mesh;
            mesh.Size = new Vector2(mesh.Size.X * 1.35f, mesh.Size.Y * 1.35f);
        }
    }

    public void ModifyMap(Map map, Attempt attempt)
    {
        foreach (Note note in map.Notes)
        {
            note.HitWindow = 31;
            note.X *= 1.35f;
            note.Y *= 1.35f;
        }
    }
}
