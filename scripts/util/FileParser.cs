using System;
using System.IO;
using System.Text;
using Godot;

public class FileParser
{
    public byte[] Buffer;
    public int Pointer;
    public long Length;

    public FileParser(string path)
    {
        Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);

        if (file == null)
        {
            Buffer = Array.Empty<byte>();
            Length = 0;
        }
        else
        {
            Length = (long)file.GetLength();
            Buffer = file.GetBuffer(Length);
            file.Close();
        }
        Pointer = 0;
    }

    public FileParser(byte[] buffer)
    {
        Buffer = buffer ?? Array.Empty<byte>();
        Length = Buffer.Length;
        Pointer = 0;
    }

    private void CheckBounds(int amount)
    {
        if (Pointer + amount > Length)
        {
            throw new EndOfStreamException($"Attempted to read {amount} bytes beyond stream length of {Length}.");
        }
    }

    public byte[] Get(int length)
    {
        CheckBounds(length);
        Pointer += length;
        return Buffer[(Pointer - length)..Pointer];
    }

    public void Skip(int length)
    {
        Pointer += length;
    }

    public void Seek(int pointer)
    {
        Pointer = pointer;
    }

    public string GetString(int length)
    {
        Pointer += length;
        return Encoding.UTF8.GetString(Buffer, Pointer - length, length);
    }

    public string GetLine()
    {
        string line = string.Empty;

        while (Pointer < Length)
        {
            Pointer++;
            string character = Encoding.UTF8.GetString(Buffer, Pointer - 1, 1);

            if (character == "\n")
                break;

            line += character;
        }

        return line;
    }

    public bool GetBool()
    {
        CheckBounds(1);
        Pointer += 1;
        return BitConverter.ToBoolean(Buffer, Pointer - 1);
    }

    public float GetFloat()
    {
        CheckBounds(4);
        Pointer += 4;
        return BitConverter.ToSingle(Buffer, Pointer - 4);
    }

    public double GetDouble()
    {
        CheckBounds(8);
        Pointer += 8;
        return BitConverter.ToDouble(Buffer, Pointer - 8);
    }

    public ushort GetUInt8()
    {
        return Get(1)[0];
    }

    public ushort GetUInt16()
    {
        CheckBounds(2);
        Pointer += 2;
        return BitConverter.ToUInt16(Buffer, Pointer - 2);
    }

    public uint GetUInt32()
    {
        CheckBounds(4);
        Pointer += 4;
        return BitConverter.ToUInt32(Buffer, Pointer - 4);
    }

    public ulong GetUInt64()
    {
        CheckBounds(8);
        Pointer += 8;
        return BitConverter.ToUInt64(Buffer, Pointer - 8);
    }
}
