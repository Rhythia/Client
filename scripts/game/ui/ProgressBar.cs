using System;
using Godot;

public partial class ProgressBar : UIComponent
{
    private SubViewport viewport;
    private TextureRect progressBarTexture;
    private TextureRect progressBarBGTexture;
    private float lastProgress = -1f;

    public override void Init()
    {
        viewport = GetNode<SubViewport>("ProgressBarViewport");
        progressBarTexture = viewport.GetNode<TextureRect>("Main");
        progressBarBGTexture = viewport.GetNode<TextureRect>("Background");
        progressBarTexture.Texture = SkinManager.Instance.Skin.ProgressImage;
        progressBarBGTexture.Texture = SkinManager.Instance.Skin.ProgressBackgroundImage;
    }

    public override void Process(double delta, Attempt attempt)
    {
        if (Mathf.IsEqualApprox(lastProgress, attempt.Progress)) return;

        lastProgress = (float)attempt.Progress;

        Vector2 progressSize = new Vector2(32 + (float)(Runner.Attempt.Progress / Runner.Attempt.Length) * 1024, 80);

        if ((int)progressSize.X != (int)progressBarTexture.Size.X)
        {
            progressBarTexture.Size = progressSize;
        }
    }
}
