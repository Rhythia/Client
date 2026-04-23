using Godot;
using System;

public partial class PanelRight : UIComponent
{
	private SubViewport viewport;
	private Label Accuracy, Hits, Misses, SimpleMisses, Sum;
	private Tween hitTween;
    private Tween missTween;
	private float _hitOpacity = 0.62f;
	private float _missOpacity = 0.62f;

	public override void _ExitTree()
    {
        if (Runner.Attempt == null) return;
		Runner.AttemptStatsUpdated -= OnStatsUpdated;
		Runner.HitResultChanged -= OnHitStateChanged;
    }

	public override void Init()
	{
		viewport = GetNode<SubViewport>("PanelRightViewport");
		viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.PanelRightBackgroundImage;
		viewport.GetNode<TextureRect>("HitsIcon").Texture = SkinManager.Instance.Skin.HitsImage;
		viewport.GetNode<TextureRect>("MissesIcon").Texture = SkinManager.Instance.Skin.MissesImage;

		Accuracy = viewport.GetNode<Label>("Accuracy");
		Hits = viewport.GetNode<Label>("Hits");
		Misses = viewport.GetNode<Label>("Misses");
		SimpleMisses = viewport.GetNode<Label>("SimpleMisses");
		Sum = viewport.GetNode<Label>("Sum");

		Hits.LabelSettings.FontColor = Color.Color8(255, 255, 255, 140);
		Misses.LabelSettings.FontColor = Color.Color8(255, 255, 255, 140);

		Runner.AttemptStatsUpdated += OnStatsUpdated;
		Runner.HitResultChanged += OnHitStateChanged;

		if (Runner.Attempt.Settings.SimpleHUD)
		{
			Godot.Collections.Array<Node> widgets = viewport.GetChildren();
            foreach (Node widget in widgets)
                (widget as CanvasItem).Visible = false;
            SimpleMisses.Visible = true;
		}
	}

    public override void _PhysicsProcess(double delta)
    {
		if (_hitOpacity > 0.62f)
		{
			_hitOpacity = Mathf.MoveToward(_hitOpacity, 0.62f, (float)delta * 1.5f);
			Hits.Modulate = new Color(1,1,1,_hitOpacity);
		}

        if (_missOpacity > 0.62f)
		{
			_missOpacity = Mathf.MoveToward(_missOpacity, 0.62f, (float)delta * 1.5f);
			Misses.Modulate = new Color(1,1,1,_missOpacity);
		}
    }

	public void OnHitStateChanged(int noteIndex, HitResult result)
	{
		switch (result)
		{
			case HitResult.Miss:
				_missOpacity = 1.0f;
				Misses.Modulate = new Color(1,1,1,1.0f);
				break;
			case HitResult.Hit:
				_hitOpacity = 1.0f;
				Hits.Modulate = new Color(1,1,1,1.0f);
				break;
		}
	}

    public void OnStatsUpdated(Attempt attempt)
	{
		Accuracy.Text = $"{(attempt.Hits + attempt.Misses == 0 ? "100.00" : $"{attempt.Accuracy:F2}")}%";
		Hits.Text = $"{attempt.Hits}";
		Misses.Text = $"{attempt.Misses}";
		SimpleMisses.Text = $"{attempt.Misses}";
		Sum.Text = Util.String.PadMagnitude(attempt.Sum.ToString());
	}
}
