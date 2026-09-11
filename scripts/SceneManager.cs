using System.Collections.Generic;
using Godot;

public partial class SceneManager : Node
{
    private static SubViewportContainer backgroundContainer;

    private static SubViewport backgroundViewport;

    private static SubViewport vrViewport;

    private static Node vrSceneContainer;

    private static Node sceneContainer;

    private static string activeScenePath;

    public static SceneManager Instance { get; private set; }

    public static Window Root;

    public static Node OverlayContainer;

    public static Dictionary<string, BaseScene> Scenes = [];

    public static BaseScene Scene;

    public static BaseSpace Space;

    public static Panel VolumePanel;

    public override void _EnterTree()
    {
        Instance = this;
        Root = GetTree().Root;
        VolumePanel = GetNode<Panel>("Volume");
    }

    public override void _Ready()
    {
        backgroundContainer = GetNode<SubViewportContainer>("Background");
        backgroundViewport = backgroundContainer.GetNode<SubViewport>("SubViewport");

        sceneContainer = this;
        OverlayContainer = this;

        if (VRNode.IsVrEnabled)
        {
            var vrMain = ResourceLoader.Load<PackedScene>("res://scenes/vr_main.tscn").Instantiate();
            AddChild(vrMain);

            var vrScreen = ResourceLoader.Load<PackedScene>("res://scenes/vr_screen.tscn").Instantiate();
            vrMain.GetNode("XROrigin3D").AddChild(vrScreen);

            vrSceneContainer = vrMain.GetNode("XROrigin3D");
            vrViewport = vrMain.GetNode<SubViewport>("XROrigin3D/VRScreen/Sprite3D/SubViewport");
            vrViewport.GuiEmbedSubwindows = true;
            sceneContainer = vrViewport;
            OverlayContainer = vrViewport;

            reparentOverlay("Settings");
            reparentOverlay("Volume");
            reparentOverlay("Cursor");
            reparentOverlay("FPSCounter");
        }

        if (!Rhythia.TempMode)
        {
            Load("res://scenes/loading.tscn");
        }
    }

    private void reparentOverlay(string nodeName)
    {
        Node overlay = GetNode(nodeName);
        RemoveChild(overlay);
        OverlayContainer.AddChild(overlay);

        if (overlay is Control control && nodeName == "Cursor")
        {
            control.ZAsRelative = false;
            control.ZIndex = 1000;
        }
    }

    public static void ReloadCurrentScene()
    {
        Load(activeScenePath, true);
    }

    public static void Load(string path, bool skipTransition = false)
    {
        bool isSceneLoaded = Scenes.TryGetValue(path, out BaseScene loadedScene);
        var newScene = isSceneLoaded ? loadedScene : (BaseScene)ResourceLoader.Load<PackedScene>(path).Instantiate();

        //         temp solution until these scenes are non-static
        if (!isSceneLoaded && newScene.Name != "SceneResults")
        {
            Scenes[path] = newScene;
        }

        var outTween = Instance.CreateTween().SetTrans(Tween.TransitionType.Quad);

        if (Scene != null)
        {
            outTween.TweenProperty(Scene.Transition, "self_modulate", Color.FromHtml("ffffffff"), skipTransition ? 0 : 0.25);
        }

        outTween.TweenCallback(Callable.From(() =>
        {
            removeScene(Scene);

            activeScenePath = path;
            Scene = newScene;
            sceneContainer = !VRNode.IsVrEnabled ? Instance : newScene is Game ? vrSceneContainer : vrViewport;

            addScene(newScene);

            newScene.Transition.SelfModulate = Color.FromHtml("ffffffff");
            Instance.CreateTween().SetTrans(Tween.TransitionType.Quad).TweenProperty(newScene.Transition, "self_modulate", Color.FromHtml("ffffff00"), skipTransition ? 0 : 0.25);
        }));
    }

    private static void addScene(BaseScene scene, bool updateSpace = true)
    {
        if (scene == null || scene.GetParent() == sceneContainer) { return; }

        if (updateSpace)
        {
            addSpace(scene.GetSpace(), scene.AddSpaceAsChild);
        }

        sceneContainer.AddChild(scene);
        scene.Load();
    }

    private static void removeScene(BaseScene scene, bool updateSpace = true)
    {
        if (scene == null || scene.GetParent() == null) { return; }

        scene.Unload();
        scene.GetParent().RemoveChild(scene);

        // also temp
        if (scene.Name == "SceneResults")
        {
            scene.QueueFree();
        }

        if (updateSpace)
        {
            removeSpace();
        }
    }

    private static void addSpace(BaseSpace space, bool addToScene = false)
    {
        Node spaceContainer = VRNode.IsVrEnabled ? vrSceneContainer : backgroundViewport;

        if (space == null || space.GetParent() == spaceContainer) { return; }

        if (addToScene)
        {
            Scene.AddChild(space);
            Scene.MoveChild(space, 0);
        }
        else
        {
            spaceContainer.AddChild(space);
        }

        space.Load();

        backgroundContainer.Visible = !addToScene && !VRNode.IsVrEnabled;
        Space = space;
    }

    private static void removeSpace()
    {
        if (Space == null) { return; }

        Space.GetParent().RemoveChild(Space);

        Space = null;
    }
}
