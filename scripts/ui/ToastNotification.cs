using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ToastNotification : Node
{
    public static ToastNotification Instance;

    private static readonly PackedScene template = GD.Load<PackedScene>("res://prefabs/notification.tscn");

    private static float maxToastWidth = 500f;

    private static int toastNextId = 0;

    private static List<string> toasts = [];

    private static readonly Dictionary<string, Task> activetweens = [];

    public override void _Ready()
    {
        Instance = this;
    }

    public static async Task Notify(string message, int severity = 0, bool multilineWrap = true)
    {
        if (SceneManager.Scene == null) { return; }

        PanelContainer notification = template.Instantiate<PanelContainer>();
        SceneManager.Scene.AddChild(notification);
        Color color = new();
        notification.Visible = true;
        notification.Name = $"NotificationToast_{toastNextId}";
        toasts.Add(notification.Name);
        toastNextId++;

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

        Label label = notification.GetNode<Label>("HBoxContainer/Label");
        label.Text = multilineWrap ? calculateMultilineWrap(maxToastWidth, message, label.GetThemeFont("font"), label.GetThemeFontSize("font_size")) : message;

        notification.GetNode<ColorRect>("HBoxContainer/Severity").Color = color;
        notification.ResetSize();
        notification.Position += Vector2.Up * (notification.Size.Y + 8);

        foreach (string toast in toasts)
        {
            if (toast == notification.Name) continue;
            PanelContainer toastPanel = (PanelContainer)SceneManager.Scene.GetNodeOrNull(toast);
            _ = queueTween(() => {
                Tween upTween = toastPanel.CreateTween();
                upTween.TweenProperty(toastPanel, "position", toastPanel.Position + Vector2.Up * (notification.Size.Y + 8), 0.15).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
                return upTween;
            }, toastPanel);
        }

        _ = queueTween(() => {
            Tween inTween = notification.CreateTween();
            inTween.TweenProperty(notification, "position", notification.Position + Vector2.Left * (notification.Size.X + 8), 0.8).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            return inTween;
        }, notification);

        await Instance.ToSignal(Instance.GetTree().CreateTimer(4), "timeout");

        _ = queueTween(() => {
            Tween outTween = notification.CreateTween();
            outTween.TweenProperty(notification, "position", notification.Position + Vector2.Right * (notification.Size.X + 8), 0.8).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            outTween.TweenCallback(Callable.From(() => {
                toasts.Remove(notification.Name);
                notification.QueueFree();
                if (toasts.Count == 0) { toastNextId = 0; }
            }));
            return outTween;
        }, notification);
    }

    private static async Task queueTween(Func<Tween> tween, PanelContainer toast)
    {
        Task previousTweenTask = activetweens.TryGetValue(toast.Name, out Task current) ? current : Task.CompletedTask;
        TaskCompletionSource taskCompletionSource = new TaskCompletionSource();
        activetweens[toast.Name] = taskCompletionSource.Task;

        await previousTweenTask;

        if (!IsInstanceValid(toast)) 
        {
            taskCompletionSource.SetResult();
            return;
        }

        Tween nextTween = tween();
        nextTween.Play();

        await toast.ToSignal(nextTween, Tween.SignalName.Finished);

        taskCompletionSource.SetResult();
    }

    private static string calculateMultilineWrap(float maxWidth, string text, Font font, int fontSize)
    {
        string[] words = text.Split(" ");
        List<string> normalizedWords = [];

        float lineWidth = 0f;

        for (int i = 0; i < words.Length; i++)
        {
            float wordWidth = font.GetStringSize(words[i], HorizontalAlignment.Left, -1, fontSize).X;

            if ((lineWidth + wordWidth) > maxWidth)
            {
                normalizedWords[^1] = "\n";
                lineWidth = 0f;
            }

            lineWidth += wordWidth;
            normalizedWords.Add(words[i]);
            normalizedWords.Add(" ");
        }

        return string.Join("", normalizedWords);
    }
}
