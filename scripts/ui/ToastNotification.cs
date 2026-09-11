using System;
using System.Threading.Tasks;
using Godot;

public partial class ToastNotification : Node
{
    public static ToastNotification Instance;

    private static readonly PackedScene template = GD.Load<PackedScene>(
        "res://prefabs/notification.tscn"
    );

    private static int activeNotifications = 0;

    public override void _Ready()
    {
        Instance = this;
    }

    public static async Task Notify(string message, int severity = 0)
    {
        if (SceneManager.Scene == null)
        {
            return;
        }

        ColorRect notification = template.Instantiate<ColorRect>();
        SceneManager.Scene.AddChild(notification);
        Color color = new();

        switch (Math.Clamp(severity, 0, 2))
        {
            case 0:
                color = Color.Color8(0, 255, 0);
                break;
            case 1:
                color = Color.Color8(255, 255, 0);
                break;
            case 2:
                color = Color.Color8(255, 0, 0);
                break;
        }

        notification.GetNode<Label>("Label").Text = message;
        notification.GetNode<ColorRect>("Severity").Color = color;
        notification.Visible = true;
        notification.Position += Vector2.Up * activeNotifications * (notification.Size.Y + 8);

        Tween inTween = notification.CreateTween();
        inTween
            .TweenProperty(
                notification,
                "position",
                notification.Position + Vector2.Left * (notification.Size.X + 8),
                0.8
            )
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        inTween.Play();

        activeNotifications++;

        await Instance.ToSignal(Instance.GetTree().CreateTimer(4), "timeout");

        Tween outTween = notification.CreateTween();
        outTween
            .TweenProperty(
                notification,
                "position",
                notification.Position + Vector2.Right * (notification.Size.X + 8),
                0.8
            )
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        outTween.TweenCallback(
            Callable.From(() =>
            {
                activeNotifications--;
                notification.QueueFree();
            })
        );
        outTween.Play();
    }
}
