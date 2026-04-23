using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Godot;
using Godot.Collections;

public class SettingsProfileConverter
{
    private static readonly int version = 1;

    public static string Serialize(SettingsProfile profile)
    {
        Dictionary data = new()
        {
            ["_Version"] = version
        };

        foreach (var property in typeof(SettingsProfile).GetProperties())
        {
            if (!property.CanRead)
            {
                continue;
            }

            if (property.GetValue(profile) is ISettingsItem item && item.SaveToDisk)
            {
                var value = item.GetVariant();

                switch (value.VariantType)
                {
                    case Variant.Type.Float:
                        data[property.Name] = Math.Round((float)value, 4);
                        break;
                    default:
                        data[property.Name] = value;
                        break;
                }
            }
        }

        return Json.Stringify(data, "\t");
    }

    public static void Deserialize(string path, SettingsProfile profile)
    {
        try
        {
            Dictionary data = (Dictionary)Json.ParseString(File.ReadAllText(path));

            foreach (var property in typeof(SettingsProfile).GetProperties())
            {
                object value = property.GetValue(profile);

                if (value is ISettingsItem item && item.SaveToDisk && data.ContainsKey(property.Name))
                {
                    item.SetVariant(data[property.Name]);
                }
            }

        }
        catch (Exception)
        {
            throw;
        }
    }
}
