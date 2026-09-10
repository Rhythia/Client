using System;
using System.Threading.Tasks;
using Godot;

public partial class ToastNotification : Node
{
    public static ToastNotification Instance;

    private static readonly PackedScene template = GD.Load<PackedScene>("res://prefabs/notification.tscn");

    private static float totalNotificationSize = 0f;

    public override void _Ready()
    {
        Instance = this;
    }

    public static async Task Notify(string message, int severity = 0)
    {
        if (SceneManager.Scene == null) { return; }

        PanelContainer notification = template.Instantiate<PanelContainer>();
        SceneManager.Instance.AddChild(notification);
        Color color = new();
        notification.Visible = true;

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

        notification.GetNode<Label>("HBoxContainer/Label").Text = message;
        notification.GetNode<ColorRect>("HBoxContainer/Severity").Color = color;
        notification.ResetSize();

        float positionY = totalNotificationSize + notification.Size.Y + 8;
        notification.Position += Vector2.Up * positionY;
        totalNotificationSize += notification.Size.Y + 8;

        Tween inTween = notification.CreateTween();
        inTween.TweenProperty(notification, "position", notification.Position + Vector2.Left * (notification.Size.X + 8), 0.8).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        inTween.Play();

        await Instance.ToSignal(Instance.GetTree().CreateTimer(4), "timeout");

        Tween outTween = notification.CreateTween();
        outTween.TweenProperty(notification, "position", notification.Position + Vector2.Right * (notification.Size.X + 8), 0.8).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        outTween.TweenCallback(Callable.From(() =>
        {
            totalNotificationSize -= notification.Size.Y + 8;
            notification.QueueFree();
        }));
        outTween.Play();
    }
}
