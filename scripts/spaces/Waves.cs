using Godot;
using System;

namespace Spaces;

public partial class Waves : BaseSpace
{
	private readonly CompressedTexture2D empty = ResourceLoader.Load<CompressedTexture2D>("res://textures/empty.png");
	private Godot.Environment environment;
	private ShaderMaterial skyMaterial;
	private ShaderMaterial waterMaterial;

	public override void _Ready()
	{
        base._Ready();

        environment = GetNode<WorldEnvironment>("WorldEnvironment").Environment;
		skyMaterial = environment.Sky.SkyMaterial as ShaderMaterial;
		waterMaterial = (GetNode<MeshInstance3D>("Water").Mesh as PlaneMesh).Material as ShaderMaterial;

		if (!Playing)
		{
			skyMaterial.SetShaderParameter("coverage", 0);
			Camera.Rotation = Vector3.Zero;
			Camera.Fov = 90;

			Tween introTween = CreateTween().SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out).SetParallel();
			introTween.TweenProperty(Camera, "rotation", Vector3.Right * Mathf.DegToRad(15), 5);
			introTween.TweenProperty(Camera, "fov", 70, 5);
			introTween.SetTrans(Tween.TransitionType.Linear);
			introTween.TweenMethod(Callable.From((float coverage) => { skyMaterial.SetShaderParameter("coverage", coverage); }), 0.0, 1.0, 8);
		}

		Tween echoTween = CreateTween().SetTrans(Tween.TransitionType.Linear);
		echoTween.TweenMethod(Callable.From((float echo) => { waterMaterial.SetShaderParameter("echo", echo); }), 0.0, 0.5, 12);
	}

	public override void _Process(double delta)
	{
        base._Process(delta);

        if (!Playing)
		{
			Viewport viewport = GetViewport();
			Vector2 centerOffset = viewport.GetMousePosition() - viewport.GetVisibleRect().Size / 2;

			environment.SkyRotation += Vector3.Up * (float)delta * centerOffset.X / 50000;
		}
	}

	public override void UpdateMap(Map map)
	{
		base.UpdateMap(map);

		if (!Playing)
        {
            skyMaterial.SetShaderParameter("image_b", skyMaterial.GetShaderParameter("image_a"));
            skyMaterial.SetShaderParameter("image_a", Cover != null ? Cover : empty);
            skyMaterial.SetShaderParameter("image_lerp", 0.0);

            Tween tween = CreateTween();
            tween.TweenMethod(Callable.From((float alpha) => {
                skyMaterial.SetShaderParameter("image_lerp", alpha);
            }), 0.0, 1.0, 0.2);
        }
	}

    public override void UpdateState(bool playing)
    {
        base.UpdateState(playing);

        if (Playing)
        {
            skyMaterial.SetShaderParameter("image_a", empty);
            skyMaterial.SetShaderParameter("image_b", empty);
		    skyMaterial.SetShaderParameter("image_lerp", 0.0);
        }
    }
}
