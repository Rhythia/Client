using Godot;

public partial class PauseHud : Control
{
	private Control progressMask;

	public override void _Ready()
	{
		progressMask = GetNode<Control>("ProgressMask");
		SetProgress(0);
	}

	public void SetProgress(float percent)
	{
		if (progressMask == null)
		{
			return;
		}

		float clamped = Mathf.Clamp(percent, 0f, 1f);
		float width = 600f * clamped;
		progressMask.Position = new Vector2(300f - (width / 2f), 0f);
		progressMask.Size = new Vector2(width, 600f);
	}
}