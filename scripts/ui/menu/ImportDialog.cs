using System;
using Godot;

public partial class ImportDialog : FileDialog
{
    public override void _Ready()
    {
        FilesSelected += (paths) =>
        {
            MapParser.BulkImport(paths);
        };
    }
}
