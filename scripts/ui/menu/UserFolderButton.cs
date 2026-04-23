using System;
using Godot;

public partial class UserFolderButton : Button
{
    public override void _Pressed()
    {
        OS.ShellShowInFileManager(Constants.USER_FOLDER);
    }
}
