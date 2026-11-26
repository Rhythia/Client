using Godot;

public interface IHudObjectModifier : IModifier
{
    void ModifyGrid(MeshInstance3D grid)
    {
        return;
    }

    void ResetGrid(MeshInstance3D grid)
    {
        return;
    }
}
