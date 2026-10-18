using System.Collections.Generic;
using Godot;

public class SettingsList<[MustBeVariant] T> : ISettingsList
{
    public SettingsList(T value)
    {
        SelectedValue = value;
        DefaultValue = value;
        Values.Add(value);
    }

    public T SelectedValue { get; set; } = default;

    public T DefaultValue { get; set; } = default;

    public List<T> Values { get; set; } = [];

    Variant ISettingsList.DefaultValue => Variant.From(DefaultValue);

    Variant ISettingsList.SelectedValue
    {
        get => Variant.From(SelectedValue);
        set => SelectedValue = value.As<T>();
    }

    IList<Variant> ISettingsList.Values => VariantUtil.ToList(Values);
}
