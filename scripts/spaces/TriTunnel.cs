using Godot;

namespace Spaces;

public partial class TriTunnel : BaseSpace
{
    private SettingsProfile settings;
    private MeshInstance3D tunnel;
    private StandardMaterial3D tunnelTexture;
    private Vector3 tunnelPosReset;
    private Color tunnelColor;
    private const float tunnel_loop_end = 148f;

    public override void _Ready()
    {
        base._Ready();

        settings = SettingsManager.Instance.Settings;
        tunnel = GetNode<MeshInstance3D>("Tunnel");
        tunnelPosReset = tunnel.Position;
        tunnelTexture = tunnel.MaterialOverride as StandardMaterial3D;
        tunnelColor = tunnelTexture.AlbedoColor;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Tunnel movement
        if (tunnel.Position.Z < tunnel_loop_end && settings.SpaceEffects)
        {
            tunnel.Position += Vector3.Back * (float)(settings.ApproachRate * delta / 6);
        }
        else
        {
            tunnel.Position = tunnelPosReset;
        }

        // Hit VFX
        tunnelTexture.AlbedoColor = settings.SpaceHitEffects ? NoteHitColor : tunnelColor;
    }
}
