using System;
using Godot;

public partial class ImportButton : Button
{
    private FileDialog importDialog;

    public override void _Ready()
    {
        importDialog = GetNode<FileDialog>("ImportDialog");
    }

    public override void _Pressed()
    {
        importDialog.Show();
    }
}
