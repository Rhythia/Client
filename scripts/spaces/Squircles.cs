using Godot;
using System;
using System.Collections.Generic;

namespace Spaces;

public partial class Squircles : BaseSpace
{
    private WorldEnvironment worldEnvironment;
    private CpuParticles3D particlesNear;
	private CpuParticles3D particlesFar;

    private Color defaultEnvironmentColor;
    private Color defaultParticleColor;

	public override void _Ready()
	{
        base._Ready();

        worldEnvironment = GetNode<WorldEnvironment>("WorldEnvironment");
        particlesNear = GetNode<CpuParticles3D>("ParticlesNear");
		particlesFar = GetNode<CpuParticles3D>("ParticlesFar");

        defaultEnvironmentColor = worldEnvironment.Environment.BackgroundColor;
        defaultParticleColor = particlesNear.Color;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Playing)
        {
            updateColor(NoteHitColor);
        }
        else
		{
			Viewport viewport = GetViewport();
			Vector2 centerOffset = viewport.GetMousePosition() - viewport.GetVisibleRect().Size / 2;

            Camera.Position = new Vector3(centerOffset.X, centerOffset.Y, 0) / 40000;
        }
    }

    public override void UpdateMap(Map map)
    {
        base.UpdateMap(map);

        Color color = defaultParticleColor;

        if (!Playing && Cover != null)
        {
            Image coverImage = Cover.GetImage();

            if (coverImage.IsCompressed())
            {
                return;
            }

            Vector3 avg = Vector3.Zero;
            int pixelCount = 0;

            for (int x = 0; x < coverImage.GetWidth(); x++)
            {
                for (int y = 0; y < coverImage.GetHeight(); y++)
                {
                    Color pixel = coverImage.GetPixel(x, y);

                    if (pixel.A == 0)
                    {
                        continue;
                    }

                    avg += new Vector3(pixel.R, pixel.G, pixel.B);
                    pixelCount++;
                }
            }

            avg /= pixelCount;
            color = new(avg.X, avg.Y, avg.Z);
        }

        updateColor(color);
    }

    private void updateColor(Color color)
    {
        Color darkened = color.Darkened(0.9f);

        worldEnvironment.Environment.BackgroundColor = Playing ? darkened : (Cover != null ? darkened : defaultEnvironmentColor);
        particlesNear.Color = color.Lightened(0.1f);
        particlesFar.Color = particlesNear.Color;
    }
}
