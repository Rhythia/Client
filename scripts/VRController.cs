using Godot;

public partial class VRController : Node
{
    private Sprite3D screen;
    private StaticBody3D screenBody;
    private SubViewport viewport;
    private Sprite2D cursor;
    private XRController3D leftController;
    private XRController3D rightController;
    private RayCast3D leftRay;
    private RayCast3D rightRay;
    private MeshInstance3D leftRayBeam;
    private MeshInstance3D rightRayBeam;
    private XRController3D activeController;
    private RayCast3D activeRay;
    private MeshInstance3D activeRayBeam;
    private Vector2 lastPosition = new(-1, -1);
    private bool hovering;
    private bool triggerPressed;
    private bool leftTriggerPressed;
    private bool rightTriggerPressed;
    private bool lastGripPressed;
    private bool grabbing;
    private bool utilityTriggerPressed;
    private bool skipTriggerPressed;
    private bool gameUiVisible;
    private bool gameSceneActive;
    private bool hasSavedScreenTransform;
    private Transform3D savedScreenTransform;
    private Vector2 grabPointLocal;
    private float grabDistance;
    private Vector3 grabTargetPosition;
    private XRCamera3D xrCamera;
    private float joystickScrollTimer;
    private const float joystick_deadzone = 0.2f;
    private const float joystick_scroll_interval = 0.12f;
    private const float grab_distance_speed = 2f;
    private const float grab_min_distance = 0.2f;
    private const float grab_camera_clearance = 0.05f;
    private const float screen_ray_length = 1000f;
    private const float max_grab_distance = 20f;
    private const float grab_scale_speed = 0.75f;
    private const float grab_position_smoothing = 14f;
    private const float min_screen_scale = 0.05f;
    private const float max_screen_scale = 1.5f;

    public override void _Ready()
    {
        screen = GetNode<Sprite3D>("Sprite3D");
        screen.MaterialOverride = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoTexture = screen.Texture
        };
        screenBody = screen.GetNode<StaticBody3D>("StaticBody3D");
        viewport = screen.GetNode<SubViewport>("SubViewport");
        cursor = viewport.GetNode<Sprite2D>("Cursor");
        cursor.Texture = SkinManager.Instance.Skin.CursorImage;
        cursor.Scale = Vector2.One * (32f / cursor.Texture.GetWidth());
        leftController = GetNode<XRController3D>("../XRLeftHand");
        rightController = GetNode<XRController3D>("../XRRightHand");
        xrCamera = GetNode<XRCamera3D>("../XRCamera3D");
        leftRay = leftController.GetNode<RayCast3D>("RayCast3D");
        rightRay = rightController.GetNode<RayCast3D>("RayCast3D");
        leftRay.TargetPosition = new Vector3(0, 0, -screen_ray_length);
        rightRay.TargetPosition = new Vector3(0, 0, -screen_ray_length);
        leftRayBeam = leftRay.GetNode<MeshInstance3D>("RayBeam");
        rightRayBeam = rightRay.GetNode<MeshInstance3D>("RayBeam");
        selectController(rightController, rightRay, rightRayBeam);
        cursor.Visible = false;
        MenuCursor.Instance.UpdateVisible(true, false);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouse)
        {
            return;
        }

        InputEvent forwardedEvent = scaleMouseEvent((InputEventMouse)@event);
        viewport.PushInput(forwardedEvent, true);
        if (forwardedEvent is InputEventMouseMotion motion)
        {
            updateMenuCursor(motion.Position);
        }

        if (SceneManager.Scene is not Game || isGameUiVisible())
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        updateMenuControllerSelection();
        updateRayBeam();

        if (SceneManager.Scene is Game game)
        {
            processGameScene(game, delta);
            return;
        }

        processMenuScene(delta);
    }

    private void updateMenuControllerSelection()
    {
        if (SceneManager.Scene is Game)
        {
            leftTriggerPressed = false;
            rightTriggerPressed = false;
            return;
        }

        bool leftPressed = leftController.IsButtonPressed("trigger_click");
        bool rightPressed = rightController.IsButtonPressed("trigger_click");

        if (leftPressed && !leftTriggerPressed)
        {
            selectController(leftController, leftRay, leftRayBeam);
        }
        else if (rightPressed && !rightTriggerPressed)
        {
            selectController(rightController, rightRay, rightRayBeam);
        }

        leftTriggerPressed = leftPressed;
        rightTriggerPressed = rightPressed;
    }

    private void updateRayBeam()
    {
        float distance = activeRay.TargetPosition.Length();

        if (activeRay.IsColliding())
        {
            distance = activeRay.ToLocal(activeRay.GetCollisionPoint()).Length();
        }

        activeRayBeam.Position = new Vector3(0, 0, -distance / 2);
        activeRayBeam.Scale = new Vector3(1, distance, 1);
    }

    private void processGameScene(Game game, double delta)
    {
        grabbing = false;
        bool enteringGame = !gameSceneActive;
        gameSceneActive = true;
        updateGameUiVisibility(game, enteringGame);
        processGameControllerInput(game);

        if (gameUiVisible)
        {
            processScreenUiInput(delta);
            return;
        }

        processGameInput(game);
    }

    private void updateGameUiVisibility(Game game, bool force = false)
    {
        bool visible = isGameUiVisible();
        if (!force && visible == gameUiVisible)
        {
            return;
        }

        gameUiVisible = visible;
        MenuCursor.Instance.UpdateVisible(visible, false);
        screen.Visible = visible;
        screenBody.CollisionLayer = visible ? 2u : 0u;

        if (visible)
        {
            savedScreenTransform = screen.GlobalTransform;
            hasSavedScreenTransform = true;
            positionScreenOnGrid(game);
        }
        else
        {
            restoreScreenTransform();

            if (!hovering)
            {
                return;
            }

            viewport.NotifyMouseExited();
            hovering = false;
        }
    }

    private void positionScreenOnGrid(Game game)
    {
        if (game.Runner.Grid == null)
        {
            return;
        }

        MeshInstance3D grid = game.Runner.Grid;
        screen.GlobalPosition = grid.GlobalPosition + grid.GlobalBasis.Z * 0.1f;
        screen.GlobalRotation = grid.GlobalRotation;

        if (grid.Mesh is QuadMesh gridMesh)
        {
            float gridHeight = gridMesh.Size.Y * grid.GlobalTransform.Basis.Scale.Y;
            screen.Scale = Vector3.One * (gridHeight / (screen.Texture.GetHeight() * screen.PixelSize));
        }
    }

    private void processMenuScene(double delta)
    {
        gameSceneActive = false;

        if (gameUiVisible)
        {
            restoreScreenTransform();
        }

        gameUiVisible = false;
        screen.Visible = true;
        screenBody.CollisionLayer = 2;
        MenuCursor.Instance.Visible = true;

        bool gripPressed = activeController.IsButtonPressed("grip");
        if (grabbing)
        {
            if (!gripPressed)
            {
                grabbing = false;
            }
            else
            {
                updateGrab(delta);
                lastGripPressed = gripPressed;
                return;
            }
        }

        if (gripPressed && !lastGripPressed && activeRay.IsColliding() && activeRay.GetCollider() == screenBody)
        {
            startGrab();
            lastGripPressed = gripPressed;
            return;
        }

        lastGripPressed = gripPressed;
        processScreenUiInput(delta);
    }

    private void restoreScreenTransform()
    {
        if (!hasSavedScreenTransform)
        {
            return;
        }

        screen.GlobalTransform = savedScreenTransform;
        hasSavedScreenTransform = false;
    }

    private void processScreenUiInput(double delta)
    {
        bool hitScreen = activeRay.IsColliding() && activeRay.GetCollider() == screenBody;

        if (!hitScreen)
        {
            cursor.Visible = false;

            if (hovering)
            {
                viewport.NotifyMouseExited();
                hovering = false;
            }

            if (triggerPressed)
            {
                pushButton(false, lastPosition, viewport);
                triggerPressed = false;
            }

            return;
        }

        Vector3 localPosition = screen.ToLocal(activeRay.GetCollisionPoint());
        Vector2 screenSize = new(
            screen.Texture.GetWidth() * screen.PixelSize,
            screen.Texture.GetHeight() * screen.PixelSize
        );
        Vector2 viewportSize = viewport.Size;
        Vector2 screenPosition = new(
            (localPosition.X / screenSize.X + 0.5f) * viewportSize.X,
            (0.5f - localPosition.Y / screenSize.Y) * viewportSize.Y
        );
        screenPosition = screenPosition.Clamp(Vector2.Zero, viewportSize);

        cursor.Position = screenPosition;
        updateMenuCursor(screenPosition);
        cursor.Visible = !MenuCursor.Instance.Visible;

        if (!hovering)
        {
            viewport.NotifyMouseEntered();
            hovering = true;
        }

        if (screenPosition != lastPosition)
        {
            viewport.PushInput(new InputEventMouseMotion
            {
                Position = screenPosition,
                GlobalPosition = screenPosition,
                Relative = screenPosition - lastPosition
            }, true);

            lastPosition = screenPosition;
        }

        bool pressed = activeController.IsButtonPressed("trigger_click");
        if (pressed != triggerPressed)
        {
            pushButton(pressed, screenPosition, viewport);
            triggerPressed = pressed;
        }

        pushJoystickScroll(delta, pressed);
    }

    private void processGameInput(Game game)
    {
        cursor.Visible = false;

        if (activeController.IsButtonPressed("trigger_click") && !skipTriggerPressed)
        {
            game.Runner.Skip();
        }

        skipTriggerPressed = activeController.IsButtonPressed("trigger_click");

        var grid = game.Runner?.Grid;
        var gridBody = grid?.GetNodeOrNull<StaticBody3D>("VRInputSurface");
        bool hitGrid = gridBody != null && activeRay.IsColliding() && activeRay.GetCollider() == gridBody;

        if (hitGrid)
        {
            Vector3 localPosition = grid.ToLocal(activeRay.GetCollisionPoint());
            game.CursorManager.SetCursorPosition(new Vector2(localPosition.X, localPosition.Y));
        }
    }

    private void processGameControllerInput(Game game)
    {
        XRController3D utilityController = activeController == leftController ? rightController : leftController;
        bool triggerPressed = utilityController.IsButtonPressed("trigger_click");

        if (triggerPressed && !utilityTriggerPressed)
        {
            game.Menu.ShowMenu(!game.Menu.Shown);
        }

        utilityTriggerPressed = triggerPressed;
    }

    private bool isGameUiVisible()
    {
        return SceneManager.Scene is Game game && (game.Menu.Shown || SettingsMenu.Instance?.Shown == true);
    }

    private InputEvent scaleMouseEvent(InputEventMouse mouseEvent)
    {
        Vector2 sourceSize = GetViewport().GetVisibleRect().Size;
        Vector2 scale = new(viewport.Size.X / sourceSize.X, viewport.Size.Y / sourceSize.Y);

        if (mouseEvent is InputEventMouseMotion motion)
        {
            return new InputEventMouseMotion
            {
                Position = motion.Position * scale,
                GlobalPosition = motion.GlobalPosition * scale,
                Relative = motion.Relative * scale,
                Velocity = motion.Velocity * scale
            };
        }

        InputEventMouseButton button = (InputEventMouseButton)mouseEvent;
        return new InputEventMouseButton
        {
            ButtonIndex = button.ButtonIndex,
            Pressed = button.Pressed,
            Position = button.Position * scale,
            GlobalPosition = button.GlobalPosition * scale,
            ButtonMask = button.ButtonMask,
            DoubleClick = button.DoubleClick
        };
    }

    private void updateMenuCursor(Vector2 position)
    {
        if (MenuCursor.Instance.Visible)
        {
            MenuCursor.Instance.Position = position - MenuCursor.Instance.Size / 2;
        }
    }

    private void startGrab()
    {
        grabbing = true;
        Vector3 collisionPoint = activeRay.GetCollisionPoint();
        Vector3 localPoint = screen.ToLocal(collisionPoint);
        grabPointLocal = new Vector2(localPoint.X, localPoint.Y);
        grabDistance = -activeRay.ToLocal(collisionPoint).Z;
        grabDistance = Mathf.Max(grabDistance, getMinimumGrabDistance());
        grabTargetPosition = screen.GlobalPosition;
        cursor.Visible = false;
        if (hovering)
        {
            viewport.NotifyMouseExited();
            hovering = false;
        }
    }

    private void updateGrab(double delta)
    {
        Vector2 joystick = activeController.GetVector2("primary");
        if (Mathf.Abs(joystick.Y) >= joystick_deadzone)
        {
            grabDistance += joystick.Y * grab_distance_speed * (float)delta;
        }
        float minimumDistance = getMinimumGrabDistance();
        float maxDistance = Mathf.Max(minimumDistance, getMaximumGrabDistance());
        grabDistance = Mathf.Clamp(grabDistance, minimumDistance, maxDistance);

        if (Mathf.Abs(joystick.X) >= joystick_deadzone)
        {
            float scale = screen.Scale.X + joystick.X * grab_scale_speed * (float)delta;
            screen.Scale = Vector3.One * Mathf.Clamp(scale, min_screen_scale, max_screen_scale);
        }

        Vector3 grabPointWorld = activeRay.GlobalPosition - activeRay.GlobalBasis.Z * grabDistance;
        Vector3 normal = (xrCamera.GlobalPosition - grabPointWorld).Normalized();
        Vector3 up = Vector3.Up - normal * Vector3.Up.Dot(normal);
        if (up.LengthSquared() < 0.001f)
        {
            up = xrCamera.GlobalBasis.Y - normal * xrCamera.GlobalBasis.Y.Dot(normal);
        }
        up = up.Normalized();
        Vector3 right = up.Cross(normal).Normalized();
        screen.GlobalRotation = new Basis(right, up, normal).GetEuler();
        grabTargetPosition = grabPointWorld - screen.GlobalBasis * new Vector3(grabPointLocal.X, grabPointLocal.Y, 0);

        float smoothing = 1f - Mathf.Exp(-grab_position_smoothing * (float)delta);
        screen.GlobalPosition = screen.GlobalPosition.Lerp(grabTargetPosition, smoothing);
    }

    private float getMinimumGrabDistance()
    {
        float minimumDistance = grab_min_distance;
        Vector3 cameraForward = -xrCamera.GlobalBasis.Z;
        Vector3 rayDirection = -activeRay.GlobalBasis.Z;
        float rayAlignment = rayDirection.Dot(cameraForward);

        if (rayAlignment > 0.001f)
        {
            float controllerDepth = (activeController.GlobalPosition - xrCamera.GlobalPosition).Dot(cameraForward);
            float rayOriginDepth = (activeRay.GlobalPosition - xrCamera.GlobalPosition).Dot(cameraForward);
            float cameraSafeDistance = (controllerDepth + grab_camera_clearance - rayOriginDepth) / rayAlignment;
            minimumDistance = Mathf.Max(minimumDistance, cameraSafeDistance);
        }

        return minimumDistance;
    }

    private float getMaximumGrabDistance()
    {
        return max_grab_distance;
    }

    private void pushJoystickScroll(double delta, bool triggerPressed)
    {
        float vertical = activeController.GetVector2("primary").Y;
        if (triggerPressed || Mathf.Abs(vertical) < joystick_deadzone)
        {
            joystickScrollTimer = 0;
            return;
        }

        joystickScrollTimer -= (float)delta;
        if (joystickScrollTimer > 0)
        {
            return;
        }

        joystickScrollTimer = joystick_scroll_interval;
        scrollHoveredControl(vertical < 0 ? 1 : -1);
    }

    private void scrollHoveredControl(int direction)
    {
        Node hoveredControl = viewport.GuiGetHoveredControl();
        while (hoveredControl != null)
        {
            if (hoveredControl is MapList mapList)
            {
                mapList.ScrollMomentum += mapList.ScrollStep * direction;
                return;
            }

            if (hoveredControl is ScrollContainer scrollContainer)
            {
                scrollContainer.ScrollVertical = Mathf.Max(0, scrollContainer.ScrollVertical + direction * 120);
                return;
            }

            hoveredControl = hoveredControl.GetParent();
        }
    }

    private void selectController(XRController3D selectedController, RayCast3D selectedRay, MeshInstance3D selectedRayBeam)
    {
        activeController = selectedController;
        activeRay = selectedRay;
        activeRayBeam = selectedRayBeam;
        leftRayBeam.Visible = selectedRay == leftRay;
        rightRayBeam.Visible = selectedRay == rightRay;
        triggerPressed = false;
        lastPosition = new(-1, -1);
        lastGripPressed = false;
        skipTriggerPressed = false;
        grabbing = false;
        joystickScrollTimer = 0;
    }

    private void pushButton(bool pressed, Vector2 position, Viewport targetViewport)
    {
        targetViewport.PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = pressed,
            Position = position,
            GlobalPosition = position
        }, true);
    }
}
