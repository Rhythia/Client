using System;
using Godot;

public partial class PanelLeft : UIComponent
{
    private SubViewport viewport;
    private ShaderMaterial multiplierProgressMaterial;
    private float currentProgress = 0;
    private Color currentColor = new Color(1, 1, 1, 1);
    private Color targetMultiplierColour = new Color(1, 1, 1, 1);
    private float targetMultiplierProgress = 0;
    private Tween multiplierTween;

    private Label score, multiplier;
    private Label altCombo;

    public override void _ExitTree()
    {
        if (Runner.Attempt == null) return;
        Runner.AttemptStatsUpdated -= OnStatsUpdated;
    }

    public override void Init()
    {
        viewport = GetNode<SubViewport>("PanelLeftViewport");
        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.PanelLeftBackgroundImage;
        score = viewport.GetNode<Label>("Score");
        multiplier = viewport.GetNode<Label>("Multiplier");
        altCombo = viewport.GetNode<Label>("ComboCount");

        multiplierProgressMaterial = viewport.GetNode<Panel>("MultiplierProgress").Material as ShaderMaterial;
        multiplierProgressMaterial.SetShaderParameter("progress", targetMultiplierProgress);
        multiplierProgressMaterial.SetShaderParameter("colour", targetMultiplierColour);
        multiplierProgressMaterial.SetShaderParameter("sides", Math.Clamp(Runner.Attempt.ComboMultiplierIncrement, 3, 32));

        Runner.AttemptStatsUpdated += OnStatsUpdated;

        bool isVisible = !Runner.Attempt.Settings.SimpleHUD && !Runner.Attempt.Settings.SuperSimpleHUD;

        Godot.Collections.Array<Node> widgets = viewport.GetChildren();
        foreach (Node widget in widgets)
        {
            (widget as CanvasItem).Visible = isVisible;
        }

        altCombo.Visible = isVisible && Runner.Attempt.Settings.AltComboCounter;
    }

    public override void _PhysicsProcess(double delta)
    {
        currentProgress = Mathf.Lerp(currentProgress, targetMultiplierProgress, Math.Min(1, (float)delta * 16));
        currentColor = currentColor.Lerp(targetMultiplierColour, (float)delta * 2);
        multiplierProgressMaterial.SetShaderParameter("progress", currentProgress);
        multiplierProgressMaterial.SetShaderParameter("colour", currentColor);
    }

    public void OnStatsUpdated(Attempt attempt)
    {
        score.Text = Util.String.PadMagnitude(attempt.Score.ToString());
        multiplier.Text = $"{attempt.ComboMultiplier}x";
        altCombo.Text = $"{attempt.Combo}";

        targetMultiplierProgress = (float)attempt.ComboMultiplierProgress / attempt.ComboMultiplierIncrement;

        if (attempt.ComboMultiplier == 8)
        {
            targetMultiplierColour = Color.Color8(255, 140, 0);
        }
        else
        {
            targetMultiplierColour = Color.Color8(255, 255, 255);
        }

        // multiplierTween.Kill();

        // multiplierTween = CreateTween().SetParallel(true);
        // multiplierTween.TweenProperty(multiplierProgressMaterial, "shader_parameter/progress", targetMultiplierProgress, 0.2f);
        // multiplierTween.TweenProperty(multiplierProgressMaterial, "shader_parameter/colour", targetMultiplierColour, 0.2f);
    }
}
