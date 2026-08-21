using Godot;

namespace Spaces;

public partial class Relic : BaseSpace
{
    private SettingsProfile settings;
    private Node3D particles;
    private GpuParticles3D aura;
    private GpuParticles3D left;
    private GpuParticles3D right;

    public override void _Ready()
    {
        base._Ready();

        settings = SettingsManager.Instance.Settings;
        particles = GetNode<Node3D>("Particles");
        aura = particles.GetNode<GpuParticles3D>("Aura");
        left = particles.GetNode<GpuParticles3D>("Left");
        right = particles.GetNode<GpuParticles3D>("Right");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        aura.Emitting = settings.SpaceEffects;
        left.Emitting = settings.SpaceEffects;
        right.Emitting = settings.SpaceEffects;
    }
}
