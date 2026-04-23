using Godot;
using System;

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
		progressBarTexture.Size = new Vector2(32 + (float)(Runner.Attempt.Progress / Runner.Attempt.MapLength) * 1024, 80);
    }
}
