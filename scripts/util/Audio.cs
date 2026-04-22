using System.Text;
using Godot;

namespace Util;

public class Audio
{
    public static AudioStream LoadStream(byte[] buffer)
    {
        AudioStream stream;

        if (buffer == null || buffer.Length < 4)
        {
            FileAccess file = FileAccess.Open("res://sounds/quiet.mp3", FileAccess.ModeFlags.Read);
            byte[] quietBuffer = file.GetBuffer((long)file.GetLength());

            file.Close();

            return new AudioStreamMP3() { Data = quietBuffer };
        }

        if (Encoding.UTF8.GetString(buffer[0..4]) == "OggS")
        {
            stream = AudioStreamOggVorbis.LoadFromBuffer(buffer);
        }
        else
        {
            stream = new AudioStreamMP3() { Data = buffer };
        }

        return stream;
    }

    public static AudioStream LoadFromFile(string path)
    {
        AudioStream stream;

        if (!System.IO.File.Exists(path))
        {
            AudioStreamMP3.LoadFromFile("res://sounds/quiet.mp3");
        }

        string ext = System.IO.Path.GetExtension(path);

        stream = ext.ToLower() switch
        {
            ".mp3" => AudioStreamMP3.LoadFromFile(path),
            ".ogg" => AudioStreamOggVorbis.LoadFromFile(path),
            _ => AudioStreamMP3.LoadFromFile("res://sounds/quiet.mp3"),
        };

        return stream;
    }
}
