using Godot;

public partial class BaseScene : Node
{
    [Export]
    public bool UseGameSpace = false;

    [Export]
    public bool UseMenuSpace = false;

    [Export]
    public bool AddSpaceAsChild = false;

    public ColorRect Transition;

    public override void _Ready()
    {
        base._Ready();

        Transition = GetNode<ColorRect>("Transition");
    }

    public virtual void Load()
    {

    }

    public virtual void Unload()
    {

    }

    public BaseSpace GetSpace()
    {
        return UseGameSpace ? SkinManager.Instance.Skin.GameSpace : UseMenuSpace ? SkinManager.Instance.Skin.MenuSpace : null;
    }
}
