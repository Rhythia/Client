using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Godot;

public partial class MapParser : Node
{
    [Signal]
    public delegate void MapsImportedEventHandler(Map[] maps);

    public static MapParser Instance;

    public override void _Ready()
    {
        Instance = this;
    }

    public static bool IsValidExt(string ext)
    {
        return ext == "phxm" || ext == "sspm" || ext == "txt";
    }

    public static void BulkImport(string[] files)
    {
        double start = Time.GetTicksUsec();
        int good = 0;
        int corrupted = 0;
        List<Map> maps = [];

        foreach (string file in files)
        {
            try
            {
                maps.Add(Decode(file, null, false, true));
                good++;
            }
            catch
            {
                corrupted++;
                continue;
            }
        }

        Logger.Log($"BULK IMPORT: {(Time.GetTicksUsec() - start) / 1000}ms; TOTAL: {good + corrupted}; CORRUPT: {corrupted}");

        SoundManager.UpdateJukeboxQueue();

        if (maps.Count > 0)
        {
            Instance.EmitSignal(SignalName.MapsImported, maps.ToArray());
        }
    }

    public static void Encode(Map map, bool logBenchmark = true)
    {
        double start = Time.GetTicksUsec();

        map.Collection = $"default";

        string mapFilePath = $"{Constants.USER_FOLDER}/maps/{map.Collection}/{map.Name}.{Constants.DEFAULT_MAP_EXT}";
        string encodePath = $"{Constants.USER_FOLDER}/cache/{Constants.DEFAULT_MAP_EXT}encode";

        if (!Directory.Exists($"{Constants.USER_FOLDER}/maps/{map.Collection}"))
        {
            Directory.CreateDirectory($"{Constants.USER_FOLDER}/maps/{map.Collection}");
        }

        if (!Directory.Exists(encodePath))
        {
            Directory.CreateDirectory(encodePath);
        }

        foreach (string file in Directory.GetFiles(encodePath))
        {
            File.Delete(file);
        }

        File.WriteAllText($"{encodePath}/metadata.json", map.EncodeMeta());

        Godot.FileAccess objects = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/cache/{Constants.DEFAULT_MAP_EXT}encode/objects.phxmo", Godot.FileAccess.ModeFlags.Write);

        /*
			uint32; ms
			1 byte; quantum
			1 byte OR int32; x
			1 byte OR int32; y
		*/

        objects.Store32(12);    // type count
        objects.Store32((uint)map.Notes.Length);    // note count

        foreach (Note note in map.Notes)
        {
            bool quantum = (int)note.X != note.X || (int)note.Y != note.Y || note.X < -1 || note.X > 1 || note.Y < -1 || note.Y > 1;

            objects.Store32((uint)note.Millisecond);
            objects.Store8(Convert.ToByte(quantum));

            if (quantum)
            {
                objects.Store32(BitConverter.SingleToUInt32Bits(note.X));
                objects.Store32(BitConverter.SingleToUInt32Bits(note.Y));
            }
            else
            {
                objects.Store8((byte)(note.X + 1)); // 0x00 = -1, 0x01 = 0, 0x02 = 1
                objects.Store8((byte)(note.Y + 1));
            }
        }

        objects.Store32(0); // timing point count
        objects.Store32(0); // brightness count
        objects.Store32(0); // contrast count
        objects.Store32(0); // saturation count
        objects.Store32(0); // blur count
        objects.Store32(0); // fov count
        objects.Store32(0); // tint count
        objects.Store32(0); // position count
        objects.Store32(0); // rotation count
        objects.Store32(0); // ar factor count
        objects.Store32(0); // text count

        objects.Close();

        if (map.AudioBuffer != null)
        {
            Godot.FileAccess audio = Godot.FileAccess.Open($"{encodePath}/audio.{map.AudioExt}", Godot.FileAccess.ModeFlags.Write);
            audio.StoreBuffer(map.AudioBuffer);
            audio.Close();
        }

        if (map.CoverBuffer != null)
        {
            Godot.FileAccess cover = Godot.FileAccess.Open($"{encodePath}/cover.png", Godot.FileAccess.ModeFlags.Write);
            cover.StoreBuffer(map.CoverBuffer);
            cover.Close();
        }

        if (map.VideoBuffer != null)
        {
            Godot.FileAccess video = Godot.FileAccess.Open($"{encodePath}/video.mp4", Godot.FileAccess.ModeFlags.Write);
            video.StoreBuffer(map.VideoBuffer);
            video.Close();
        }

        if (File.Exists(mapFilePath))
        {
            File.Delete(mapFilePath);
        }

        ZipFile.CreateFromDirectory(encodePath, mapFilePath, CompressionLevel.NoCompression, false);
        map.Hash = MapCache.GetMd5Checksum(mapFilePath);
        map.FilePath = mapFilePath;
        MapCache.InsertMap(map);
        MapCache.Load(false);

        foreach (string filePath in Directory.GetFiles(encodePath))
        {
            File.Delete(filePath);
        }

        if (logBenchmark)
        {
            Logger.Log($"ENCODING {Constants.DEFAULT_MAP_EXT.ToUpper()}: {(Time.GetTicksUsec() - start) / 1000}ms");
        }
    }

    public static Map Decode(string path, string audio = null, bool logBenchmark = false, bool save = false)
    {
        if (!File.Exists(path))
        {
            ToastNotification.Notify($"Invalid file path", 2);
            throw Logger.Error($"Invalid file path ({path})");
        }

        Map map;
        string ext = path.GetExtension();
        double start = Time.GetTicksUsec();

        if (!IsValidExt(ext))
        {
            ToastNotification.Notify("Unsupported file format", 1);
            throw Logger.Error($"Unsupported file format ({ext})");
        }

        switch (ext)
        {
            case "phxm":
                map = PHXM(path);
                break;
            case "sspm":
                map = SSPM(path);
                break;
            case "txt":
                map = SSMapV1(path, audio);
                break;
            default:
                map = new();
                break;
        }

        if (logBenchmark)
        {
            Logger.Log($"DECODING {ext.ToUpper()}: {(Time.GetTicksUsec() - start) / 1000}ms");
        }

        if (save)
        {
            Encode(map, logBenchmark);
        }

        return map;
    }

    public static Map SSMapV1(string path, string audioPath = null)
    {
        string name = path.Split("\\")[^1].TrimSuffix(".txt");
        Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        Map map;

        try
        {
            string[] split = file.GetLine().Split(",");
            Note[] notes = new Note[split.Length - 1];
            byte[] audioBuffer = null;

            for (int i = 1; i < split.Length; i++)
            {
                string[] subsplit = split[i].Split("|");

                notes[i - 1] = new Note(i - 1, subsplit[2].ToInt(), -subsplit[0].ToFloat() + 1, subsplit[1].ToFloat() - 1);
            }

            if (audioPath != null)
            {
                Godot.FileAccess audio = Godot.FileAccess.Open(audioPath, Godot.FileAccess.ModeFlags.Read);

                audioBuffer = audio.GetBuffer((long)audio.GetLength());

                audio.Close();
            }

            map = new(path, notes, null, "", name, audioBuffer: audioBuffer);
        }
        catch (Exception exception)
        {
            ToastNotification.Notify($"SSMapV1 file corrupted", 2);
            Logger.Error(exception);
            throw;
        }

        file.Close();

        return map;
    }

    public static Map SSPM(string path)
    {
        FileParser file = new(path);
        Map map;

        try
        {
            if (file.GetString(4) != "SS+m")
            {
                throw new("Incorrect file signature");
            }

            ushort version = file.GetUInt16(); // SSPM version

            if (version == 1)
            {
                map = sspmV1(file, path);
            }
            else if (version == 2)
            {
                map = sspmV2(file, path);
            }
            else
            {
                throw new("Invalid SSPM version");
            }

        }
        catch (Exception exception)
        {
            ToastNotification.Notify($"SSPM file corrupted", 2);
            Logger.Error(exception);
            throw;
        }

        return map;
    }

    public static Map SSPM(byte[] bytes)
    {
        var file = new FileParser(bytes);
        Map map;

        try
        {
            if (file.GetString(4) != "SS+m")
            {
                throw new("Incorrect file signature");
            }

            ushort version = file.GetUInt16(); // SSPM version

            if (version == 1)
            {
                map = sspmV1(file);
            }
            else if (version == 2)
            {
                map = sspmV2(file);
            }
            else
            {
                throw new("Invalid SSPM version");
            }
        }
        catch (Exception exception)
        {
            ToastNotification.Notify($"SSPM file corrupted", 2);
            Logger.Error(exception);
            throw;
        }

        return map;
    }

    private static Map sspmV1(FileParser file, string path = null)
    {
        Map map;

        try
        {
            file.Skip(2); // reserved
            string id = file.GetLine();

            string[] mapName = file.GetLine().Split(" - ", 2);

            string artist = null;
            string song = null;

            if (mapName.Length == 1)
            {
                song = mapName[0].StripEdges();
            }
            else
            {
                artist = mapName[0].StripEdges();
                song = mapName[1].StripEdges();
            }

            string[] mappers = file.GetLine().Split(['&', ',']);

            uint mapLength = file.GetUInt32();
            uint noteCount = file.GetUInt32();

            int difficulty = file.GetUInt8();

            bool hasCover = file.GetUInt8() == 2;
            byte[] coverBuffer = null;
            if (hasCover)
            {
                int coverByteLength = (int)file.GetUInt64();
                coverBuffer = file.Get(coverByteLength);
            }

            bool hasAudio = file.GetBool();
            byte[] audioBuffer = null;
            if (hasAudio)
            {
                int audioByteLength = (int)file.GetUInt64();
                audioBuffer = file.Get(audioByteLength);
            }

            Note[] notes = new Note[noteCount];

            for (int i = 0; i < noteCount; i++)
            {
                int millisecond = (int)file.GetUInt32();

                bool isQuantum = file.GetBool();

                float x;
                float y;

                if (isQuantum)
                {
                    x = file.GetFloat();
                    y = file.GetFloat();
                }
                else
                {
                    x = file.GetUInt8();
                    y = file.GetUInt8();
                }

                notes[i] = new Note(i, millisecond, x - 1, -y + 1);
            }

            Array.Sort(notes);

            for (int i = 0; i < notes.Length; i++)
            {
                notes[i].Index = i;
            }

            map = new(path ?? $"{Constants.USER_FOLDER}/maps/{song}_temp.sspm", notes, id, artist, song, 0, mappers, difficulty, null, (int)mapLength, audioBuffer, coverBuffer);
        }
        catch (Exception exception)
        {
            ToastNotification.Notify($"SSPMV1 file corrupted", 2);
            Logger.Error(exception);
            throw;
        }

        return map;
    }

    private static Map sspmV2(FileParser file, string path = null)
    {
        Map map;

        try
        {
            file.Skip(4);   // reserved
            file.Skip(20);  // hash

            uint mapLength = file.GetUInt32();
            uint noteCount = file.GetUInt32();

            file.Skip(4);   // marker count

            int difficulty = file.Get(1)[0];

            file.Skip(2);   // map rating

            bool hasAudio = file.GetBool();
            bool hasCover = file.GetBool();

            file.Skip(1);   // 1mod

            ulong customDataOffset = file.GetUInt64();
            ulong customDataLength = file.GetUInt64();

            ulong audioByteOffset = file.GetUInt64();
            ulong audioByteLength = file.GetUInt64();

            ulong coverByteOffset = file.GetUInt64();
            ulong coverByteLength = file.GetUInt64();

            file.Skip(16);  // marker definitions offset & marker definitions length

            ulong markerByteOffset = file.GetUInt64();

            file.Skip(8);   // marker byte length (can just use notecount)

            uint mapIdLength = file.GetUInt16();
            string id = file.GetString((int)mapIdLength);

            uint mapNameLength = file.GetUInt16();
            string[] mapName = file.GetString((int)mapNameLength).Split(" - ", 2);

            string artist = null;
            string song = null;

            if (mapName.Length == 1)
            {
                song = mapName[0].StripEdges();
            }
            else
            {
                artist = mapName[0].StripEdges();
                song = mapName[1].StripEdges();
            }

            uint songNameLength = file.GetUInt16();

            file.Skip((int)songNameLength); // why is this different?

            uint mapperCount = file.GetUInt16();
            string[] mappers = new string[mapperCount];

            for (int i = 0; i < mapperCount; i++)
            {
                uint mapperNameLength = file.GetUInt16();

                mappers[i] = file.GetString((int)mapperNameLength);
            }

            byte[] audioBuffer = null;
            byte[] coverBuffer = null;
            string difficultyName = null;

            file.Seek((int)customDataOffset);
            file.Skip(2);   // skip number of fields, only care about diff name

            if (file.GetString(file.GetUInt16()) == "difficulty_name")
            {
                int length = 0;

                switch (file.Get(1)[0])
                {
                    case 9:
                        length = file.GetUInt16();
                        break;
                    case 11:
                        length = (int)file.GetUInt32();
                        break;
                }

                difficultyName = file.GetString(length);
            }

            if (hasAudio)
            {
                file.Seek((int)audioByteOffset);
                audioBuffer = file.Get((int)audioByteLength);
            }

            if (hasCover)
            {
                file.Seek((int)coverByteOffset);
                coverBuffer = file.Get((int)coverByteLength);
            }

            file.Seek((int)markerByteOffset);

            Note[] notes = new Note[noteCount];

            for (int i = 0; i < noteCount; i++)
            {
                int millisecond = (int)file.GetUInt32();

                file.Skip(1);   // marker type, always note

                bool isQuantum = file.GetBool();
                float x;
                float y;

                if (isQuantum)
                {
                    x = file.GetFloat();
                    y = file.GetFloat();
                }
                else
                {
                    x = file.Get(1)[0];
                    y = file.Get(1)[0];
                }

                notes[i] = new Note(0, millisecond, x - 1, -y + 1);
            }

            Array.Sort(notes);

            for (int i = 0; i < notes.Length; i++)
            {
                notes[i].Index = i;
            }

            map = new(path, notes, id, artist, song, 0, mappers, difficulty, difficultyName, (int)mapLength, audioBuffer, coverBuffer);
        }
        catch (Exception exception)
        {
            ToastNotification.Notify($"SSPMV2 file corrupted", 2);
            Logger.Error(exception);
            throw;
        }

        return map;
    }

    public static Map PHXM(string path)
    {
        string decodePath = $"{Constants.USER_FOLDER}/cache/{Constants.DEFAULT_MAP_EXT}decode";

        if (!Directory.Exists(decodePath))
        {
            Directory.CreateDirectory(decodePath);
        }

        foreach (string filePath in Directory.GetFiles(decodePath))
        {
            File.Delete(filePath);
        }

        Map map;

        try
        {
            using var file = ZipFile.OpenRead(path);

            byte[] getEntryBuffer(string entryName)
            {
                ZipArchiveEntry entry = file.GetEntry(entryName) ?? throw new($"Entry {entryName} for map {path} is missing!");
                Stream stream = entry.Open();
                MemoryStream memoryStream = new();

                stream.CopyTo(memoryStream);
                stream.Dispose();

                byte[] buffer = memoryStream.ToArray();
                memoryStream.Dispose();

                return buffer;
            }

            byte[] metaBuffer = getEntryBuffer("metadata.json");
            byte[] objectsBuffer = getEntryBuffer("objects.phxmo");
            byte[] audioBuffer = null;
            byte[] coverBuffer = null;
            byte[] videoBuffer = null;

            Godot.Collections.Dictionary metadata = (Godot.Collections.Dictionary)Json.ParseString(Encoding.UTF8.GetString(metaBuffer));
            FileParser objects = new(objectsBuffer);

            if ((bool)metadata["HasAudio"])
            {
                audioBuffer = getEntryBuffer($"audio.{metadata["AudioExt"]}");
            }

            if ((bool)metadata["HasCover"])
            {
                coverBuffer = getEntryBuffer("cover.png");
            }

            if ((bool)metadata["HasVideo"])
            {
                videoBuffer = getEntryBuffer("video.mp4");
            }

            uint typeCount = objects.GetUInt32();
            uint noteCount = objects.GetUInt32();

            Note[] notes = new Note[noteCount];

            for (int i = 0; i < noteCount; i++)
            {
                int ms = (int)objects.GetUInt32();
                bool quantum = objects.GetBool();
                float x;
                float y;

                if (quantum)
                {
                    x = objects.GetFloat();
                    y = objects.GetFloat();
                }
                else
                {
                    x = objects.Get(1)[0] - 1;
                    y = objects.Get(1)[0] - 1;
                }

                notes[i] = new(i, ms, x, y);
            }

            file.Dispose();

            // temp
            metadata.TryGetValue("ArtistLink", out Variant artistLink);
            metadata.TryGetValue("ArtistPlatform", out Variant artistPlatform);

            map = new(
                path,
                notes,
                (string)metadata["ID"],
                (string)metadata["Artist"],
                (string)metadata["Title"],
                0,
                (string[])metadata["Mappers"],
                (int)metadata["Difficulty"],
                (string)metadata["DifficultyName"],
                (int)metadata["Length"],
                audioBuffer,
                coverBuffer,
                videoBuffer,
                false,
                (string)artistLink ?? "",
                (string)artistPlatform ?? ""
            );
        }
        catch (Exception exception)
        {
            ToastNotification.Notify($"PHXM file corrupted", 2);
            Logger.Error(exception);
            throw;
        }

        return map;
    }

    public static Note[] DecodePHXMO(string path)
    {
        FileParser objects = new(path);

        uint typeCount = objects.GetUInt32();
        uint noteCount = objects.GetUInt32();

        Note[] notes = new Note[noteCount];

        for (int i = 0; i < noteCount; i++)
        {
            int ms = (int)objects.GetUInt32();
            bool quantum = objects.GetBool();
            float x;
            float y;

            if (quantum)
            {
                x = objects.GetFloat();
                y = objects.GetFloat();
            }
            else
            {
                x = objects.Get(1)[0] - 1;
                y = objects.Get(1)[0] - 1;
            }

            notes[i] = new(i, ms, x, y);
        }

        return notes;
    }

    public static Note[] DecodePHXMO(byte[] buffer)
    {
        FileParser objects = new(buffer);

        uint typeCount = objects.GetUInt32();
        uint noteCount = objects.GetUInt32();

        Note[] notes = new Note[noteCount];

        for (int i = 0; i < noteCount; i++)
        {
            int ms = (int)objects.GetUInt32();
            bool quantum = objects.GetBool();
            float x;
            float y;

            if (quantum)
            {
                x = objects.GetFloat();
                y = objects.GetFloat();
            }
            else
            {
                x = objects.Get(1)[0] - 1;
                y = objects.Get(1)[0] - 1;
            }

            notes[i] = new(i, ms, x, y);
        }

        return notes;
    }
}
