using Godot;

namespace Spaces;

public partial class Tunnel : BaseSpace
{
	private SettingsProfile settings;
	private StandardMaterial3D tileMaterial;
	private StandardMaterial3D ringMaterialA;
	private StandardMaterial3D ringMaterialB;
	private Node3D rings;
    
	private const float ring_loop_end = 52.5f;

	public override void _Ready()
	{
		base._Ready();
		
		settings = SettingsManager.Instance.Settings;
		rings = GetNode<Node3D>("Rings");
		
		tileMaterial = (GetNode<MeshInstance3D>("Road").Mesh as PlaneMesh).Material as StandardMaterial3D;
		ringMaterialA = (rings.GetChild<MeshInstance3D>(0).Mesh as PlaneMesh).Material as StandardMaterial3D;
		ringMaterialB = (rings.GetChild<MeshInstance3D>(1).Mesh as PlaneMesh).Material as StandardMaterial3D;
	}

	public override void _Process(double delta)
	{    
		base._Process(delta);
		
		// Ring movement
		rings.Position = Vector3.Back * (float)(Time.GetTicksMsec() / 1000f * settings.ApproachRate / 2) % ring_loop_end;
		
		// Hit FX
		tileMaterial.AlbedoColor = NoteHitColor;
		ringMaterialA.AlbedoColor = NoteHitColor;
		ringMaterialB.AlbedoColor = NoteHitColor;
	}
}
