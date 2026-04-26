using Godot;

namespace Spaces;

public partial class Tunnel : BaseSpace
{
	private SettingsProfile settings;
	private StandardMaterial3D tileMaterial;
	private StandardMaterial3D ringMaterialA;
	private StandardMaterial3D ringMaterialB;
	private Node3D rings;
	private Vector3 ringLoopEnd = new(0, 0, 53);
	private Vector3 ringPosReset;

	public override void _Ready()
	{
		base._Ready();
		
		settings = SettingsManager.Instance.Settings;
		rings = GetNode<Node3D>("Rings");
		ringPosReset = rings.Position;
		
		tileMaterial = (GetNode<MeshInstance3D>("Road").Mesh as PlaneMesh).Material as StandardMaterial3D;
		ringMaterialA = (rings.GetChild<MeshInstance3D>(0).Mesh as PlaneMesh).Material as StandardMaterial3D;
		ringMaterialB = (rings.GetChild<MeshInstance3D>(1).Mesh as PlaneMesh).Material as StandardMaterial3D;
	}

	public override void _Process(double delta)
	{    
		base._Process(delta);
		
		// Ring movement
		if (rings.Position.Round() != ringLoopEnd)
		{
			rings.Position += new Vector3 (0,0,(float)settings.ApproachRate) * (float)delta / 2 ;
		}
		else
		{
			rings.Position = ringPosReset;
		};
		
		// Hit FX
		tileMaterial.AlbedoColor = NoteHitColor;
		ringMaterialA.AlbedoColor = NoteHitColor;
		ringMaterialB.AlbedoColor = NoteHitColor;
	}
}
