using Godot;

namespace Spaces;

public partial class Relic : BaseSpace
{
    private SettingsProfile settings;
    private Node3D Particles;
    private GpuParticles3D Aura;
    private GpuParticles3D Left;
    private GpuParticles3D Right;

    public override void _Ready()
    {
        base._Ready();

        settings = SettingsManager.Instance.Settings;
        Particles = GetNode<Node3D>("Particles");
        Aura = Particles.GetNode<GpuParticles3D>("Aura");
        Left = Particles.GetNode<GpuParticles3D>("Left");
        Right = Particles.GetNode<GpuParticles3D>("Right");

    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!settings.SpaceEffects)
        {
            Aura.Emitting = false;
            Left.Emitting = false;
            Right.Emitting = false;
        }
        else
        {
            Aura.Emitting = true;
            Left.Emitting = true;
            Right.Emitting = true;
        }
        ;
    }
}
