using Godot;

namespace Spaces;

public partial class Galaxy : BaseSpace
{
	private SettingsProfile settings;
	private Environment environment;
	// private Node3D planet;
	// private Vector3 planetStartPos;
	private StandardMaterial3D tileMaterial;
	private Color fogReset = new(0, 0, 0);

	public override void _Ready()
	{
		base._Ready();
		
		settings = SettingsManager.Instance.Settings;
		environment = WorldEnvironment.Environment;
		tileMaterial = (GetNode<MeshInstance3D>("Road").Mesh as PlaneMesh).Material as StandardMaterial3D;
		
		// planet = GetNode<Node3D>("Planet");
		// planetStartPos = planet.Position;
	}

	public override void _Process(double delta)
	{    
		base._Process(delta);
		
		// sky rotation
		environment.SkyRotation += (Vector3.Down + Vector3.Right) * (float)delta / 100;
		
		// tile movement
		tileMaterial.Uv1Offset += Vector3.Up * (float)delta / 2;
		
		// planet floating
		//planet.Position = planetStartPos + Vector3.Up * (float)Math.Sin(Time.GetTicksMsec() / 1000f ); 
		
		// fog
		environment.FogLightColor = settings.SpaceHitEffects ? NoteHitColor : fogReset;
	}
}
