using Godot;

[GlobalClass]
public partial class SkinProfile : GodotObject
{
    public SkinConfig Config { get; set; } = new();

    public ImageTexture CursorImage { get; set; } = new();

    public ImageTexture GridImage { get; set; } = new();

    public ImageTexture PanelLeftBackgroundImage { get; set; } = new();

    public ImageTexture PanelRightBackgroundImage { get; set; } = new();

    public ImageTexture HealthImage { get; set; } = new();

    public ImageTexture HealthBackgroundImage { get; set; } = new();

    public ImageTexture ProgressImage { get; set; } = new();

    public ImageTexture ProgressBackgroundImage { get; set; } = new();

    public ImageTexture HitsImage { get; set; } = new();

    public ImageTexture MissesImage { get; set; } = new();

    public ImageTexture MissFeedbackImage { get; set; } = new();

    public ImageTexture SettingsButtonImage { get; set; } = new();

    public ImageTexture OpenFolderButtonImage { get; set; } = new();

    public ImageTexture ImportButtonImage { get; set; } = new();

    public ImageTexture RandomButtonImage { get; set; } = new();

    public ImageTexture FilterButtonImage { get; set; } = new();

    public ImageTexture SortButtonImage { get; set; } = new();

    public ImageTexture AuthorButtonImage { get; set; } = new();

    public ImageTexture SearchButtonImage { get; set; } = new();

    public ImageTexture LayoutListButtonImage { get; set; } = new();

    public ImageTexture LayoutGridButtonImage { get; set; } = new();

    public ImageTexture SpeedPresetMinusMinusButtonImage { get; set; } = new();

    public ImageTexture SpeedPresetMinusButtonImage { get; set; } = new();

    public ImageTexture SpeedPresetMiddleButtonImage { get; set; } = new();

    public ImageTexture SpeedPresetPlusButtonImage { get; set; } = new();

    public ImageTexture SpeedPresetPlusPlusButtonImage { get; set; } = new();

    public ImageTexture PlayButtonImage { get; set; } = new();

    public ImageTexture FavoriteButtonImage { get; set; } = new();

    public ImageTexture UnfavoriteButtonImage { get; set; } = new();

    public ImageTexture CopyButtonImage { get; set; } = new();

    public ImageTexture DeleteButtonImage { get; set; } = new();

    public ImageTexture AddVideoButtonImage { get; set; } = new();

    public ImageTexture RemoveVideoButtonImage { get; set; } = new();

    public ImageTexture GrabberNormalImage { get; set; } = new();

    public ImageTexture GrabberPressedImage { get; set; } = new();

    public ImageTexture GrabberTickImage { get; set; } = new();

    public ImageTexture JukeboxPlayImage { get; set; } = new();

    public ImageTexture JukeboxPauseImage { get; set; } = new();

    public ImageTexture JukeboxSkipImage { get; set; } = new();

    public ImageTexture FavoriteImage { get; set; } = new();

    public ImageTexture MapListMaskImage { get; set; } = new();

    public ImageTexture MapListSelectionCursorImage { get; set; } = new();

    public ImageTexture MapListScrollBarTopImage { get; set; } = new();

    public ImageTexture MapListScrollBarMiddleImage { get; set; } = new();

    public ImageTexture MapListScrollBarBottomImage { get; set; } = new();

    public ImageTexture MapListScrollBarBackgroundTopImage { get; set; } = new();

    public ImageTexture MapListScrollBarBackgroundMiddleImage { get; set; } = new();

    public ImageTexture MapListScrollBarBackgroundBottomImage { get; set; } = new();

    public ImageTexture MapListGridCoverBackgroundImage { get; set; } = new();

    public ImageTexture MapInfoCoverBackgroundImage { get; set; } = new();

    public ImageTexture ModNoFailImage { get; set; } = new();

    public ImageTexture ModGhostImage { get; set; } = new();

    public ImageTexture BackgroundTileImage { get; set; } = new();

    public Shader BackgroundTileShader { get; set; } = new();

    public Shader MapButtonCoverShader { get; set; } = new();

    public byte[] HitSoundBuffer { get; set; } = [];

    public byte[] MissSoundBuffer { get; set; } = [];

    public byte[] FailSoundBuffer { get; set; } = [];

    public byte[] MenuMusicBuffer { get; set; } = [];

    public Color[] NoteColors { get; set; } = [new(0xff0059), new(0xffd8e6)];

    public ArrayMesh NoteMesh { get; set; } = new();

    public BaseSpace MenuSpace { get; set; }

    public BaseSpace GameSpace { get; set; }
}
