using System;
using Godot;

public partial class PanelRight : UIComponent
{
    private SubViewport viewport;
    private Label accuracy, hits, misses, simpleMisses, sum;
    private Tween hitTween;
    private Tween missTween;
    private float hitOpacity = 0.62f;
    private float missOpacity = 0.62f;

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

        accuracy = viewport.GetNode<Label>("Accuracy");
        hits = viewport.GetNode<Label>("Hits");
        misses = viewport.GetNode<Label>("Misses");
        simpleMisses = viewport.GetNode<Label>("SimpleMisses");
        sum = viewport.GetNode<Label>("Sum");

        // Hits.LabelSettings.FontColor = Color.Color8(255, 255, 255, 140);
        // Misses.LabelSettings.FontColor = Color.Color8(255, 255, 255, 140);

        Runner.AttemptStatsUpdated += OnStatsUpdated;
        Runner.HitResultChanged += OnHitStateChanged;

        bool isVisible = !Runner.Attempt.Settings.SimpleHUD && !Runner.Attempt.Settings.SuperSimpleHUD;

        Godot.Collections.Array<Node> widgets = viewport.GetChildren();
        foreach (Node widget in widgets)
        {
            (widget as CanvasItem).Visible = isVisible;
        }

        simpleMisses.Visible = !isVisible;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (hitOpacity > 0.62f)
        {
            hitOpacity = Mathf.MoveToward(hitOpacity, 0.62f, (float)delta * 1.5f);
            hits.Modulate = new Color(1, 1, 1, hitOpacity);
        }

        if (missOpacity > 0.62f)
        {
            missOpacity = Mathf.MoveToward(missOpacity, 0.62f, (float)delta * 1.5f);
            misses.Modulate = new Color(1, 1, 1, missOpacity);
        }
    }

    public void OnHitStateChanged(int noteIndex, HitResult result)
    {
        switch (result)
        {
            case HitResult.Miss:
                missOpacity = 1.0f;
                misses.Modulate = new Color(1, 1, 1, 1.0f);
                break;
            case HitResult.Hit:
                hitOpacity = 1.0f;
                hits.Modulate = new Color(1, 1, 1, 1.0f);
                break;
        }
    }

    public void OnStatsUpdated(Attempt attempt)
    {
        accuracy.Text = $"{(attempt.Hits + attempt.Misses == 0 ? "100.00" : $"{attempt.Accuracy:F2}")}%";
        hits.Text = $"{attempt.Hits}";
        misses.Text = $"{attempt.Misses}";
        simpleMisses.Text = $"{attempt.Misses}";
        sum.Text = Util.String.PadMagnitude(attempt.Sum.ToString());
    }
}
