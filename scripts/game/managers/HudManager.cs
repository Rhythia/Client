using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class HudManager : Node
{
	[Export] public Runner Runner;
	
	private List<IUIComponent> components = [];

	private List<IUIComponent> FindAllComponents(Node root)
	{
		List<IUIComponent> comps = new();

		foreach (Node child in root.GetChildren())
		{
			if (child is IUIComponent component)
				comps.Add(component);
			
			comps.AddRange(FindAllComponents(child));
		}

		return comps;
	}

    public void Init()
    {
        Runner ??= GetParent<Runner>();
		components = FindAllComponents(this);

		foreach (var component in components)
		{
			component.Runner = Runner;
			component.Init();
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
}