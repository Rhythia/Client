using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Godot;

public static class RhsImporter
{
    private const int BG_WIDTH = 1920;
    private const int BG_HEIGHT = 1080;
    private const double SPACE_UNITS_PER_SCREEN_HEIGHT = 5.25;

    public static string Import(string rhsPath)
    {
        string baseName = Path.GetFileNameWithoutExtension(rhsPath).Replace('.', '-');
        string skinName = baseName;
        string skinDir = $"{Constants.USER_FOLDER}/skins/{skinName}";

        if (Directory.Exists(skinDir))
        {
            int i = 2;
            while (Directory.Exists($"{Constants.USER_FOLDER}/skins/{baseName} ({i})"))
                i++;
            skinName = $"{baseName} ({i})";
            skinDir = $"{Constants.USER_FOLDER}/skins/{skinName}";
        }

        string gameDir = $"{skinDir}/game";
        Directory.CreateDirectory(gameDir);

        using var archive = ZipFile.OpenRead(rhsPath);

        var configEntry = archive.GetEntry("config");
        if (configEntry == null)
        {
            Logger.Error("RHS import failed: no config entry");
            return null;
        }

        JsonElement config;
        using (var reader = new StreamReader(configEntry.Open()))
        {
            config = JsonSerializer.Deserialize<JsonElement>(reader.ReadToEnd());
        }

        ExtractFirstImage(archive, "cursorSkin/", $"{gameDir}/cursor.png");
        ExtractFirstImage(archive, "cursorTrailSkin/", $"{gameDir}/cursor_trail.png");
        ExtractFirstImage(archive, "borderSkin/", $"{gameDir}/grid.png");
        ExtractColorSet(archive, skinName);
        ExtractFirstFile(archive, "hitSound/", $"{skinDir}/hit.mp3");

        bool hasBackground = ComposeBackground(archive, config, gameDir);

        string gameSpace = hasBackground ? "void" : "grid";
        WriteConfig(skinDir, skinName, gameSpace);

        Logger.Log($"Imported RHS skin: {skinName}");
        return skinName;
    }

    private static JsonElement Unwrap(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var el))
            return default;
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("Value", out var inner))
            return inner;
        return el;
    }

    private static int UnwrapInt(JsonElement root, string property, int fallback = 0)
    {
        var el = Unwrap(root, property);
        return el.ValueKind == JsonValueKind.Number ? el.GetInt32() : fallback;
    }

    private static double UnwrapDouble(JsonElement root, string property, double fallback = 0)
    {
        var el = Unwrap(root, property);
        return el.ValueKind == JsonValueKind.Number ? el.GetDouble() : fallback;
    }

    private static void ExtractFirstImage(ZipArchive archive, string prefix, string destPath)
    {
        var entry = archive.Entries.FirstOrDefault(e =>
            e.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && e.Length > 0
            && IsImageExt(Path.GetExtension(e.Name)));

        if (entry == null) return;

        var image = LoadImage(ReadEntry(entry), entry.Name);
        image?.SavePng(destPath);
    }

    private static void ExtractFirstFile(ZipArchive archive, string prefix, string destPath)
    {
        var entry = archive.Entries.FirstOrDefault(e =>
            e.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && e.Length > 0);

        if (entry == null) return;

        using var src = entry.Open();
        using var dst = File.Create(destPath);
        src.CopyTo(dst);
    }

    private static void ExtractColorSet(ZipArchive archive, string skinName)
    {
        var entry = archive.Entries.FirstOrDefault(e =>
            e.FullName.StartsWith("colorSet/", StringComparison.OrdinalIgnoreCase)
            && e.Length > 0);

        if (entry == null) return;

        string dir = $"{Constants.USER_FOLDER}/colorsets";
        Directory.CreateDirectory(dir);

        using var reader = new StreamReader(entry.Open());
        File.WriteAllText($"{dir}/{skinName}.txt", reader.ReadToEnd().Trim());
    }

    private static bool ComposeBackground(ZipArchive archive, JsonElement config, string gameDir)
    {
        var bgEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("backgrounds/", StringComparison.OrdinalIgnoreCase)
                && e.Length > 0
                && IsImageExt(Path.GetExtension(e.Name)))
            .ToList();

        if (bgEntries.Count == 0) return false;

        int bgR = UnwrapInt(config, "BackgroundRed");
        int bgG = UnwrapInt(config, "BackgroundGreen");
        int bgB = UnwrapInt(config, "BackgroundBlue");

        var canvas = Image.Create(BG_WIDTH, BG_HEIGHT, false, Image.Format.Rgba8);
        canvas.Fill(new Color(bgR / 255f, bgG / 255f, bgB / 255f, 1f));

        var layers = Unwrap(config, "BackgroundImages");
        if (layers.ValueKind == JsonValueKind.Array)
        {
            int idx = 0;
            foreach (var layer in layers.EnumerateArray())
            {
                DrawLayer(canvas, archive, bgEntries, layer, idx);
                idx++;
            }
        }
        else if (bgEntries.Count > 0)
        {
            var img = LoadImage(ReadEntry(bgEntries[0]), bgEntries[0].Name);
            if (img != null)
            {
                img.Resize(BG_WIDTH, BG_HEIGHT, Image.Interpolation.Lanczos);
                canvas.BlendRect(img, new Rect2I(0, 0, BG_WIDTH, BG_HEIGHT), Vector2I.Zero);
            }
        }

        canvas.SavePng($"{gameDir}/background.png");
        return true;
    }

    private static void DrawLayer(
        Image canvas,
        ZipArchive archive,
        System.Collections.Generic.List<ZipArchiveEntry> bgEntries,
        JsonElement layer,
        int index)
    {
        double tintOpacity = layer.TryGetProperty("TintOpacity", out var opEl) ? opEl.GetDouble() : 1.0;
        if (tintOpacity <= 0) return;

        string layerPath = layer.TryGetProperty("Path", out var pEl) ? pEl.GetString() ?? "" : "";
        string fileName = Path.GetFileName(layerPath);

        var entry = bgEntries.FirstOrDefault(e =>
            e.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        entry ??= bgEntries.FirstOrDefault(e =>
            e.FullName.StartsWith($"backgrounds/{index}/", StringComparison.OrdinalIgnoreCase));

        if (entry == null) return;

        var img = LoadImage(ReadEntry(entry), entry.Name);
        if (img == null) return;

        int placement = layer.TryGetProperty("Placement", out var plEl) ? plEl.GetInt32() : 0;
        double centerX, centerY, boxW, boxH;

        if (placement == 1)
        {
            double ppu = BG_HEIGHT / SPACE_UNITS_PER_SCREEN_HEIGHT;
            centerX = BG_WIDTH / 2.0 + GetDouble(layer, "SpaceX") * ppu;
            centerY = BG_HEIGHT / 2.0 - GetDouble(layer, "SpaceY") * ppu;
            boxW = GetDouble(layer, "SpaceWidth", 8) * ppu;
            boxH = GetDouble(layer, "SpaceHeight", 8) * ppu;
        }
        else
        {
            centerX = GetDouble(layer, "CenterX", 0.5) * BG_WIDTH;
            centerY = GetDouble(layer, "CenterY", 0.5) * BG_HEIGHT;
            boxW = GetDouble(layer, "ScaleX", 1) * BG_WIDTH;
            boxH = GetDouble(layer, "ScaleY", 1) * BG_HEIGHT;
        }

        double ratio = Math.Min(boxW / img.GetWidth(), boxH / img.GetHeight());
        int drawW = Math.Max(1, (int)Math.Round(img.GetWidth() * ratio));
        int drawH = Math.Max(1, (int)Math.Round(img.GetHeight() * ratio));

        var resized = (Image)img.Duplicate();
        resized.Resize(drawW, drawH, Image.Interpolation.Lanczos);

        float tR = (float)(GetDouble(layer, "TintRed", 255) / 255.0);
        float tG = (float)(GetDouble(layer, "TintGreen", 255) / 255.0);
        float tB = (float)(GetDouble(layer, "TintBlue", 255) / 255.0);
        float tA = (float)tintOpacity;

        int dstX = (int)Math.Round(centerX - drawW / 2.0);
        int dstY = (int)Math.Round(centerY - drawH / 2.0);

        bool needsTint = tR < 0.999f || tG < 0.999f || tB < 0.999f || tA < 0.999f;

        if (needsTint)
            BlendTinted(canvas, resized, dstX, dstY, tR, tG, tB, tA);
        else
            canvas.BlendRect(resized, new Rect2I(Vector2I.Zero, resized.GetSize()), new Vector2I(dstX, dstY));
    }

    private static void BlendTinted(Image dst, Image src, int dstX, int dstY,
        float tR, float tG, float tB, float tA)
    {
        int srcW = src.GetWidth(), srcH = src.GetHeight();
        int dstW = dst.GetWidth(), dstH = dst.GetHeight();

        int x0 = Math.Max(0, -dstX);
        int y0 = Math.Max(0, -dstY);
        int x1 = Math.Min(srcW, dstW - dstX);
        int y1 = Math.Min(srcH, dstH - dstY);

        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                var s = src.GetPixel(x, y);
                float a = s.A * tA;
                if (a <= 0f) continue;

                var d = dst.GetPixel(dstX + x, dstY + y);
                float outA = a + d.A * (1f - a);
                if (outA <= 0f) continue;

                float inv = d.A * (1f - a);
                dst.SetPixel(dstX + x, dstY + y, new Color(
                    (s.R * tR * a + d.R * inv) / outA,
                    (s.G * tG * a + d.G * inv) / outA,
                    (s.B * tB * a + d.B * inv) / outA,
                    outA));
            }
        }
    }

    private static void WriteConfig(string skinDir, string colorsetName, string gameSpace)
    {
        string toml =
            $"theme_color = \"c90045\"\n" +
            $"note_colors = \"{colorsetName}\"\n" +
            $"note_mesh = \"squircle\"\n" +
            $"game_space = \"{gameSpace}\"\n" +
            $"menu_space = \"waves\"\n";
        File.WriteAllText($"{skinDir}/config.toml", toml);
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        using var s = entry.Open();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private static Image LoadImage(byte[] data, string filename)
    {
        var image = new Image();
        string ext = Path.GetExtension(filename).ToLowerInvariant();

        Error err = ext switch
        {
            ".jpg" or ".jpeg" => image.LoadJpgFromBuffer(data),
            ".bmp" => image.LoadBmpFromBuffer(data),
            ".webp" => image.LoadWebpFromBuffer(data),
            ".tga" => image.LoadTgaFromBuffer(data),
            _ => image.LoadPngFromBuffer(data),
        };

        return err == Error.Ok ? image : null;
    }

    private static bool IsImageExt(string ext) => ext?.ToLowerInvariant() switch
    {
        ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp" or ".tga" or ".gif" => true,
        _ => false,
    };

    private static double GetDouble(JsonElement el, string prop, double fallback = 0)
    {
        return el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble()
            : fallback;
    }
}
