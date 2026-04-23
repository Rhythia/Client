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
        float width = 320f * clamped;
        progressMask.OffsetRight = width / 2;
        progressMask.OffsetLeft = -width / 2;
    }
}
