using System;
using System.Collections.Generic;
using Godot;

public interface ISettingsItem
{
    event Action<Variant> Updated;

    Variant GetVariant();

    void SetVariant(Variant variant);

    void Reset();

    string Id { get; }

    Type Type { get; }

    string Title { get; }

    string Description { get; }

    string Placeholder { get; }

    bool LineTranslation { get; }

    bool ListTranslation { get; }

    ISettingsList List { get; }

    SettingsSlider Slider { get; }

    SettingsSection Section { get; }

    bool Editable { get; }

    bool Visible { get; }

    bool SaveToDisk { get; }
}
