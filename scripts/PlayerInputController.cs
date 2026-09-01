using System;
using Godot;

public partial class PlayerInputController : Node
{
    // I'm not sure if it's planned that the player can change keybindings
    // So these bindings are hardcoded (for now) -thom

    /// <summary>
    /// Fired when the mouse moves.
    /// <para>
    /// First parameter is relative movement (delta),
    /// second parameter is absolute screen position.
    /// </para>
    /// </summary>
    public event Action<Vector2, Vector2> OnMouseMove;

    public event Action OnTogglePaused;
    public event Action OnRestartPressed;
    public event Action OnToggleReplayViewerVisibility;
    public event Action OnToggleShowOrthonogalCamera;
    public event Action OnPauseOrSkipPressed;
    public event Action OnPauseOrSkipReleased;
    public event Action OnToggleFade;
    public event Action OnTogglePushback;
    public event Action<bool> OnLeftMouseButton;
    public bool IsEnabled { get; private set; } = true;

    public override void _Input(InputEvent @event)
    {
        if (!IsEnabled) return;

        handleInput(@event);
    }

    private void handleInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseMotion:
            case InputEventMouseButton { ButtonIndex: MouseButton.Left }:
                handleMouseInput(@event);
                break;
            case InputEventKey { PhysicalKeycode: Key.Escape }:
            case InputEventKey { PhysicalKeycode: Key.F2 }:
            case InputEventKey { PhysicalKeycode: Key.F3 }:
            case InputEventKey { PhysicalKeycode: Key.Space }:
            case InputEventKey { PhysicalKeycode: Key.F }:
            case InputEventKey { PhysicalKeycode: Key.P }:
            case InputEventKey { PhysicalKeycode: Key.Quoteleft }:
                handleKeyboardInput(@event);
                break;
        }
    }

    private void handleMouseInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
            OnMouseMove?.Invoke(motion.Relative, motion.Position);

        if (@event is InputEventMouseButton mouseButton)
        {
            if (!mouseButton.Pressed || mouseButton.DoubleClick) return;

            OnLeftMouseButton?.Invoke(mouseButton.Pressed);
        }
    }

    private void handleKeyboardInput(InputEvent @event)
    {
        var key = (InputEventKey)@event;

        // Functionality with pressing and releasing

        if (key.Echo) return;

        switch (key)
        {
            case { PhysicalKeycode: Key.Space }:
                if (key.Pressed)
                {
                    OnPauseOrSkipPressed?.Invoke();
                }
                else if (!key.Pressed)
                {
                    OnPauseOrSkipReleased?.Invoke();
                }
                break;
        }

        // Functionality with only pressing

        if (!key.Pressed || key.Echo) return;

        switch (key)
        {
            case { PhysicalKeycode: Key.Escape }:
                OnTogglePaused?.Invoke();
                break;
            case { PhysicalKeycode: Key.Quoteleft }:
                OnRestartPressed?.Invoke();
                break;
            case { PhysicalKeycode: Key.F2 }:
                OnToggleReplayViewerVisibility?.Invoke();
                break;
            case { PhysicalKeycode: Key.F3 }:
                OnToggleShowOrthonogalCamera?.Invoke();
                break;
            case { PhysicalKeycode: Key.F }:
                OnToggleFade?.Invoke();
                break;
            case { PhysicalKeycode: Key.P }:
                OnTogglePushback?.Invoke();
                break;
        }
    }

    public void ToggleState() => IsEnabled = !IsEnabled;
}
