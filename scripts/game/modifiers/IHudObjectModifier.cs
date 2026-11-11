using Godot;

public interface IHudObjectModifier : IModifier
{
    void ModifyGrid(MeshInstance3D grid)
    {
        return;
    }

    void ModifyGridGuides(MeshInstance3D gridGuides)
    {
        return;
    }
}
