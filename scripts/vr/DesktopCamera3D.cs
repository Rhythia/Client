using Godot;
using System;

// sync with DesktopCameraFOV setting
public partial class DesktopCamera3D : Camera3D
{
	// Called when the node enters the scene tree for the first time.
	Camera3D cameraToFollow;
	public override void _Ready()
	{
		cameraToFollow = GetNode<Camera3D>("../../../SubViewport/XROrigin3D/XRCamera3D");
		Fov = (float)SettingsManager.Instance.Settings.DesktopCameraFOV.Value;

		// sync with DesktopCameraFOV setting changes
		SettingsManager.Instance.Settings.DesktopCameraFOV.UpdateAction += UpdateFov;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (cameraToFollow != null)
		{
			GlobalTransform = cameraToFollow.GlobalTransform;
		}
	}

	// Called when the DesktopCameraFOV setting changes.
	public void UpdateFov(double newValue, bool isInit)
	{
		Fov = (float)newValue;
	}
}
