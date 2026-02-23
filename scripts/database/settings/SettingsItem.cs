using System;
using System.Collections.Generic;
using Godot;

public class SettingsItem<[MustBeVariant] T> : ISettingsItem
{
    private bool _init = false;

    public SettingsItem(T value)
    {
        Value = value;
        DefaultValue = value;
    }

    public event Action<Variant> Updated;

    public Type Type { get; } = typeof(T);

    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public T DefaultValue { get; private set; } = default;

    public List<SettingsButton> Buttons { get; set; } = new();

    public SettingsList<T> List { get; set; } = default;

    public SettingsSlider Slider { get; set; } = default;

    public SettingsSection Section { get; set; } = SettingsSection.None;

    public bool Editable { get; set; } = true;

    public bool Visible { get; set; } = true;

    public bool SaveToDisk { get; set; } = true;

    public T Value
    {
        get;
        set
        {
            field = value;

            List?.SelectedValue = value;

            UpdateAction?.Invoke(value, !_init);
            Updated?.Invoke(Variant.From(Value));

            if (!_init && Id != "")
            {
                _init = true;
            }
        }
    }

    public Variant GetVariant() => Variant.From(Value);

    public void SetVariant(Variant value)
    {
        Value = value.As<T>();
    }

    public Action<T, bool> UpdateAction { get; set; } = null;

    public static implicit operator T(SettingsItem<T> item) => item.Value;

    ISettingsList ISettingsItem.List => List;
}
