using System.Collections.Generic;
using Godot;
using Util;

public partial class HudManager : Node
{
    [Export] public Runner Runner;

    private List<IUIComponent> components = [];
    private readonly List<Sprite3D> modifierIcons = [];
    private Sprite3D modifierTemplate;

    public void Init()
    {
        Runner ??= GetParent<Runner>();
        components = findAllComponents(this);

        modifierTemplate ??= GetNode<Sprite3D>("TemplateModifier");
        modifierTemplate.Visible = false;

        foreach (var icon in modifierIcons)
        {
            icon.QueueFree();
        }
        modifierIcons.Clear();

        foreach (var component in components)
        {
            component.Runner = Runner;
            component.Init();
        }
    }

    public void DisplayModifier(Modifier mod)
    {
        Sprite3D icon = (Sprite3D)modifierTemplate.Duplicate();
        icon.Texture = Misc.GetModIcon(mod.Name);
        icon.Visible = true;
        modifierTemplate.GetParent().AddChild(icon);
        modifierIcons.Add(icon);
        positionModifiers();
    }

    private void positionModifiers()
    {
        for (int i = 0; i < modifierIcons.Count; i++)
        {
            Vector3 pos = modifierTemplate.Position;
            pos.X = (i - (modifierIcons.Count - 1) / 2f) * 0.75f;
            modifierIcons[i].Position = pos;
        }
    }

    public override void _Process(double delta)
    {
        if (Runner?.Attempt == null) return;

        foreach (var component in components)
        {
            component.Process(delta, Runner.Attempt);
        }
    }

    private List<IUIComponent> findAllComponents(Node root)
    {
        List<IUIComponent> comps = new();

        foreach (Node child in root.GetChildren())
        {
            if (child is IUIComponent component)
                comps.Add(component);

            comps.AddRange(findAllComponents(child));
        }

        return comps;
    }
}
