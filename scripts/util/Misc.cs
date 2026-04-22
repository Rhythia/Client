using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;

namespace Util;

public class Misc
{
    public static GodotObject OBJParser = (GodotObject)GD.Load<GDScript>("res://scripts/util/OBJParser.gd").New();

    public static ImageTexture GetModIcon(string mod)
    {
        ImageTexture tex;

        switch (mod)
        {
            case "NoFail":
                tex = SkinManager.Instance.Skin.ModNoFailImage;
                break;
            case "Ghost":
                tex = SkinManager.Instance.Skin.ModGhostImage;
                break;
            default:
                tex = new();
                break;
        }

        return tex;
    }

    public static void CopyProperties(Node node, Node reference)
    {
        foreach (Godot.Collections.Dictionary property in reference.GetPropertyList())
        {
            string key = (string)property["name"];

            if (key == "size" || key == "script")
            {
                continue;
            }

            node.Set(key, reference.Get(key));
        }
    }

    public static void CopyReference(Node node, Node reference)
    {
        Util.Misc.CopyProperties(node, reference);

        reference.ReplaceBy(node);
        reference.QueueFree();
    }

    public static Image LoadImageFromBuffer(byte[] buffer)
    {
        if (buffer == null || buffer.Length < 4)
        {
            return null;
        }

        Image img = new Image();

        bool isPng = buffer[0] == 137 && buffer[1] == 80 && buffer[2] == 78 && buffer[3] == 71;
        if (isPng && img.LoadPngFromBuffer(buffer) == Error.Ok)
        {
            return img;
        }

        bool isJpeg = buffer.Length >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
        if (isJpeg && img.LoadJpgFromBuffer(buffer) == Error.Ok)
        {
            return img;
        }

        bool isBmp = buffer.Length >= 2 && buffer[0] == 0x42 && buffer[1] == 0x4D;
        if (isBmp && img.LoadBmpFromBuffer(buffer) == Error.Ok)
        {
            return img;
        }

        bool isWebp = buffer.Length >= 12
            && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46
            && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50;
        if (isWebp && img.LoadWebpFromBuffer(buffer) == Error.Ok)
        {
            return img;
        }

        return null;
    }

    public static Color ParseColor(string hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) { return fallback; }

        try
        {
            hex = hex.Trim();
            if (!hex.StartsWith('#')) { hex = "#" + hex; }
            return Color.FromHtml(hex);
        }
        catch
        {
            Logger.Log($"Invalid color: {hex} (reset to default value)");
            return fallback;
        }
    }

    public static float ParseFloatInput(string input, float fallback = 0f)
    {
        if (string.IsNullOrWhiteSpace(input)) { return fallback; }

        string normalized = input.Replace(',', '.');
        if (float.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }

        return fallback;
    }
}
