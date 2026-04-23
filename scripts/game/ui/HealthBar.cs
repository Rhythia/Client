using Godot;
using System;

public partial class HealthBar : UIComponent
{
	private SubViewport viewport;
	private TextureRect healthBarTexture;
	private TextureRect healthBarBGTexture;
	private Tween tween;
	private Vector2 _targetSize = new Vector2(1088,80);
	private Vector2 _currentSize = new Vector2(1088,80);

    public override void _ExitTree()
    {
        if (Runner.Attempt == null) return;
		Runner.AttemptStatsUpdated -= OnStatsUpdated;
		tween?.Kill();
    }

	public override void Init()
	{
		viewport = GetNode<SubViewport>("HealthViewport");
		healthBarTexture = viewport.GetNode<TextureRect>("Main");
		healthBarBGTexture = viewport.GetNode<TextureRect>("Background");
		healthBarTexture.Texture = SkinManager.Instance.Skin.HealthImage;
		healthBarBGTexture.Texture = SkinManager.Instance.Skin.HealthBackgroundImage;

		Runner.AttemptStatsUpdated += OnStatsUpdated;
	}

    public override void _PhysicsProcess(double delta)
    {
		_currentSize = _currentSize.Lerp(_targetSize, (float)delta * 15f);
        healthBarTexture.Size = _currentSize;
    }

	public void OnStatsUpdated(Attempt attempt)
	{
		float targetWidth = 32 + (float)attempt.Health * 10.24f;
		_targetSize = new Vector2(targetWidth, 80);

		// tween?.Kill();
		// tween = CreateTween();
		// tween.TweenProperty(healthBarTexture, "size", targetSize, 0.2f);

		if (!attempt.IsReplay && attempt.Health <= 0)
		{
			healthBarTexture.Modulate = Color.Color8(255, 255, 255, 128);
			healthBarBGTexture.Modulate = healthBarTexture.Modulate;
		}
	}
}
