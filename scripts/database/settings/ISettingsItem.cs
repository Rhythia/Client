using System;
using System.Collections.Generic;
using Godot;

public interface ISettingsItem
{
    event Action<Variant> Updated;

    Variant GetVariant();

    void SetVariant(Variant variant);

    string Id { get; }

    Type Type { get; }

    string Title { get; }

    string Description { get; }

    ISettingsList List { get; }

    SettingsSlider Slider { get; }

    SettingsSection Section { get; }

    bool Editable { get; }

    bool Visible { get; }

    bool SaveToDisk { get; }
}
