using System.Linq;
using Godot;
using Godot.Collections;

public partial class VRNode : Node3D
{
    public static VRNode Instance { get; private set; }
    public static bool IsVrEnabled;

    [Signal]
    public delegate void FocusLostEventHandler();
    [Signal]
    public delegate void FocusGainedEventHandler();
    [Signal]
    public delegate void PoseRecenteredEventHandler();

    private static XRInterface xrInterface;

    [Export]
    public int MaximumRefreshRate { get; set; } = 90;
    private bool xrIsFocused;

    public override void _Ready()
    {
        Instance = this;
        xrInterface = (OpenXRInterface)XRServer.FindInterface("OpenXR");
        if (xrInterface != null && xrInterface.IsInitialized())
        {
            IsVrEnabled = true;
            GD.Print("OpenXR instantiated successfully.");

            // Enable XR on our viewport
            GetViewport().UseXR = true;

            // Make sure v-sync is off, v-sync is handled by OpenXR
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

            // Enable VRS
            if (RenderingServer.GetRenderingDevice() != null)
            {
                GetViewport().VrsMode = Viewport.VrsModeEnum.XR;
            }
            else if ((int)ProjectSettings.GetSetting("xr/openxr/foveation_level") == 0)
            {
                GD.PushWarning("OpenXR: Recommend setting Foveation level to High in Project Settings");
            }

            // Connect the OpenXR events
            ((OpenXRInterface)xrInterface).SessionBegun += onOpenXRSessionBegun;
            ((OpenXRInterface)xrInterface).SessionStopping += onOpenXRStopping;
            ((OpenXRInterface)xrInterface).PoseRecentered += onOpenXRPoseRecentered;
            ((OpenXRInterface)xrInterface).SessionVisible += onOpenXRVisibleState;
            ((OpenXRInterface)xrInterface).SessionFocussed += onOpenXRFocusedState;
            ((OpenXRInterface)xrInterface).InstanceExiting += onOpenXRInstanceExiting;

        }
        else
        {
            // We couldn't start OpenXR.
            GD.Print("OpenXR not instantiated!");
        }
    }

    private void onOpenXRSessionBegun()
    {
        GD.Print("OpenXR session begun");
    }

    private void onOpenXRStopping()
    {
        // Our session is being stopped.
        GD.Print("OpenXR is stopping");
        // some send this when you take ur headset off. great..
        //SceneManager.Root.PropagateNotification((int)NotificationWMCloseRequest);
    }

    private void onOpenXRPoseRecentered()
    {
        EmitSignal(SignalName.PoseRecentered);
    }

    private void onOpenXRVisibleState()
    {
        GD.Print("OpenXR session visible");
        xrIsFocused = false;
        EmitSignal(SignalName.FocusLost);
    }

    private void onOpenXRFocusedState()
    {
        GD.Print("OpenXR gained focus");
        xrIsFocused = true;

        // Un-pause our game
        GetTree().Paused = false;

        EmitSignal(SignalName.FocusGained);
    }
    private void onOpenXRInstanceExiting()
    {
        SceneManager.Root.PropagateNotification((int)NotificationWMCloseRequest);
        GD.Print("OpenXR instance exiting");
    }
}
