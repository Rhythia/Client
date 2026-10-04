using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;

public class AutoplayHandler
{
    private readonly List<AutoNote> processedData = [];
    private int lastLoadedNote;

    private const float max_shift_multiplier = 0.25f;
    private const int cache_version = 1;

    private readonly struct AutoNote(float x, float y, double millisecond)
    {
        public readonly float X = x;
        public readonly float Y = y;
        public readonly double Millisecond = millisecond;

        public Vector2 Position => new(X, Y);

        public AutoNote WithPosition(Vector2 position) => new(position.X, position.Y, Millisecond);
    }

    public AutoplayHandler(Attempt attempt)
    {
        var notes = attempt.Map.Notes.Select(note => new AutoNote(note.X, note.Y, note.Millisecond)).ToList();

        if (notes.Count == 0)
        {
            return;
        }

        notes = mergeSimultaneousNotes(notes);

        string cachePath = getCachePath(notes);
        if (tryLoadCache(cachePath, notes.Count))
        {
            return;
        }

        // this can be used for difficulty calculation
        List<AutoNote> preprocessedData = initialPreprocess(notes);
        List<AutoNote> secondaryPreprocessedData = compressStacks(preprocessedData);
        shiftPreprocess(notes, preprocessedData, secondaryPreprocessedData);
        saveCache(cachePath);
    }

    public void Reset(double progress)
    {
        int index = lowerBound(processedData, progress);
        lastLoadedNote = index < processedData.Count && processedData[index].Millisecond == progress ? index : Math.Max(0, index - 1);
    }

    public Vector2 GetCursorPosition(double progress)
    {
        if (processedData.Count == 0)
        {
            return Vector2.Zero;
        }

        if (progress < processedData[lastLoadedNote].Millisecond)
        {
            Reset(progress);
        }

        while (lastLoadedNote + 1 < processedData.Count)
        {
            AutoNote note = processedData[lastLoadedNote + 1];

            if (note.Millisecond > progress)
            {
                break;
            }

            lastLoadedNote++;
        }

        AutoNote note0 = processedData[Math.Max(lastLoadedNote - 1, 0)];
        AutoNote note1 = processedData[lastLoadedNote];
        AutoNote note2 = processedData[Math.Min(lastLoadedNote + 1, processedData.Count - 1)];
        AutoNote note3 = processedData[Math.Min(lastLoadedNote + 2, processedData.Count - 1)];

        return getSplinePosition(note0, note1, note2, note3, progress).Clamp(-Constants.BOUNDS, Constants.BOUNDS);
    }

    private List<AutoNote> initialPreprocess(List<AutoNote> notes)
    {
        List<AutoNote> preprocessedData = [];
        int i = 0;

        while (i < notes.Count)
        {
            AutoNote note = notes[i];
            i++;

            List<AutoNote> collected = [note];

            while (i < notes.Count)
            {
                AutoNote next = notes[i];

                if (Math.Abs(next.Millisecond - note.Millisecond) <= 5 && checkHit(note, next, hitboxSize() * 0.9f))
                {
                    collected.Add(next);
                    i++;
                }
                else
                {
                    break;
                }
            }

            Vector2 avgPos = Vector2.Zero;

            foreach (AutoNote collectedNote in collected)
            {
                avgPos += collectedNote.Position;
            }

            avgPos /= collected.Count;

            if (preprocessedData.Count > 0)
            {
                AutoNote prevData = preprocessedData[^1];
                Vector2 prevDataPos = prevData.Position;
                float timeElapsed = (float)(Math.Abs(prevData.Millisecond - note.Millisecond) / 1000.0);

                if (timeElapsed > 0)
                {
                    float speed = prevDataPos.DistanceTo(avgPos) / (timeElapsed * 100);
                    avgPos *= 0.8f + (sigmoid(speed * 5) - 0.5f) * 2 * 0.2f;
                }

                if (i > 2 && preprocessedData.Count >= 2)
                {
                    AutoNote prevPrevData = preprocessedData[^2];
                    Vector2 dir1 = (prevDataPos - prevPrevData.Position).Normalized();
                    Vector2 dir2 = (avgPos - prevDataPos).Normalized();

                    avgPos *= 1 + Math.Max(-dir1.Dot(dir2) - 0.5f, 0) * (1 / (timeElapsed * 20 + 1));
                }
            }

            AutoNote newNote = new(avgPos.X, avgPos.Y, note.Millisecond);
            AutoNote previousNote = notes[Math.Max(i - 2, 0)];

            if (collected.Count == 1 && note.X == previousNote.X && note.Y == previousNote.Y && preprocessedData.Count > 0)
            {
                newNote = new(preprocessedData[^1].X, preprocessedData[^1].Y, newNote.Millisecond);
            }

            preprocessedData.Add(newNote);
        }

        return preprocessedData;
    }

    private List<AutoNote> compressStacks(List<AutoNote> notes)
    {
        List<AutoNote> compressed = [];

        for (int i = 0; i < notes.Count; i++)
        {
            AutoNote note = notes[i];
            if (i > 0 && i + 1 < notes.Count && notes[i - 1].Position == note.Position && notes[i + 1].Position == note.Position)
            {
                continue;
            }

            compressed.Add(note);
        }

        return compressed;
    }

    private void shiftPreprocess(List<AutoNote> originalNotes, List<AutoNote> preprocessedData, List<AutoNote> secondaryPreprocessedData)
    {
        float maxRange = hitboxSize() * max_shift_multiplier;

        for (int i = 0; i + 1 < secondaryPreprocessedData.Count; i++)
        {
            AutoNote note0 = secondaryPreprocessedData[Math.Max(i - 2, 0)];
            AutoNote note1 = secondaryPreprocessedData[Math.Max(i - 1, 0)];
            AutoNote note2 = secondaryPreprocessedData[i];
            AutoNote note3 = secondaryPreprocessedData[i + 1];
            AutoNote note4 = secondaryPreprocessedData[Math.Min(i + 2, secondaryPreprocessedData.Count - 1)];

            if (note1.Position == note2.Position || note2.Position == note3.Position)
            {
                processedData.Add(note2);
                continue;
            }

            Vector2 position = getSplinePosition(note0, note1, note3, note4, note2.Millisecond);
            Vector2 shift = clampShift(position - note2.Position, maxRange);
            AutoNote shifted = note2.WithPosition(note2.Position + shift);
            List<AutoNote> validation = [.. processedData.TakeLast(3), shifted, .. secondaryPreprocessedData.Skip(i + 1).Take(3)];

            bool valid = true;
            double low = validation[Math.Min(2, validation.Count - 1)].Millisecond;
            double high = validation[Math.Max(0, validation.Count - 3)].Millisecond;

            for (int noteIndex = lowerBound(originalNotes, low); noteIndex < originalNotes.Count; noteIndex++)
            {
                AutoNote note = originalNotes[noteIndex];
                if (note.Millisecond > high)
                    break;

                bool hit =
                    checkHit(note, getCursorPositionFromNotes(validation, note.Millisecond + 1), hitboxSize() * 0.9f)
                    || checkHit(note, getCursorPositionFromNotes(validation, note.Millisecond + 5), hitboxSize() * 0.9f);
                if (!hit)
                {
                    valid = false;
                    break;
                }
            }

            processedData.Add(valid ? shifted : note2);
        }

        processedData.Add(preprocessedData[^1]);
    }

    private Vector2 getCursorPositionFromNotes(List<AutoNote> noteData, double elapsed)
    {
        if (noteData.Count == 0)
        {
            return Vector2.Zero;
        }

        int tempLastLoadedNote = 0;

        while (tempLastLoadedNote + 1 < noteData.Count)
        {
            AutoNote note = noteData[tempLastLoadedNote + 1];

            if (note.Millisecond > elapsed)
            {
                break;
            }

            tempLastLoadedNote++;
        }

        AutoNote note0 = noteData[Math.Max(tempLastLoadedNote - 1, 0)];
        AutoNote note1 = noteData[tempLastLoadedNote];
        AutoNote note2 = noteData[Math.Min(tempLastLoadedNote + 1, noteData.Count - 1)];
        AutoNote note3 = noteData[Math.Min(tempLastLoadedNote + 2, noteData.Count - 1)];

        return getSplinePosition(note0, note1, note2, note3, elapsed).Clamp(-Constants.BOUNDS, Constants.BOUNDS);
    }

    private static bool checkHit(AutoNote notePos, AutoNote cursorPos, float size) => checkHit(notePos, cursorPos.Position, size);

    private static bool checkHit(AutoNote notePos, Vector2 cursorPos, float size)
    {
        Vector2 diff = (notePos.Position - cursorPos).Abs();
        return Math.Max(diff.X, diff.Y) < size;
    }

    private static Vector2 getSplinePosition(AutoNote note0, AutoNote note1, AutoNote note2, AutoNote note3, double time)
    {
        double segmentDuration = note2.Millisecond - note1.Millisecond;

        if (segmentDuration <= 0)
        {
            return note1.Position;
        }

        if (note1.Position == note2.Position)
        {
            return note1.Position;
        }

        float u = (float)Math.Clamp((time - note1.Millisecond) / segmentDuration, 0, 1);

        return new(
            catmullRomRaw(note0.X, note1.X, note2.X, note3.X, u, note0.Millisecond, note1.Millisecond, note2.Millisecond, note3.Millisecond),
            catmullRomRaw(note0.Y, note1.Y, note2.Y, note3.Y, u, note0.Millisecond, note1.Millisecond, note2.Millisecond, note3.Millisecond)
        );
    }

    private static float catmullRomRaw(
        float pos0,
        float pos1,
        float pos2,
        float pos3,
        float u,
        double time0,
        double time1,
        double time2,
        double time3
    )
    {
        const float spline_alpha = 0.4f;
        const float spline_tension = -1f;

        float t01 = Mathf.Pow(Math.Max(Math.Abs((float)(time0 - time1)), 1), spline_alpha);
        float t12 = Mathf.Pow(Math.Max(Math.Abs((float)(time1 - time2)), 1), spline_alpha);
        float t23 = Mathf.Pow(Math.Max(Math.Abs((float)(time2 - time3)), 1), spline_alpha);

        float m1 = (1 - spline_tension) * (pos2 - pos1 + t12 * ((pos1 - pos0) / t01 - (pos2 - pos0) / (t01 + t12)));
        float m2 = (1 - spline_tension) * (pos2 - pos1 + t12 * ((pos3 - pos2) / t23 - (pos3 - pos1) / (t12 + t23)));

        if (!float.IsFinite(m1))
        {
            m1 = 0;
        }

        if (!float.IsFinite(m2))
        {
            m2 = 0;
        }

        float u2 = u * u;

        return (2 * (pos1 - pos2) + m1 + m2) * u2 * u + (-3 * (pos1 - pos2) - m1 - m1 - m2) * u2 + m1 * u + pos1;
    }

    private static Vector2 clampShift(Vector2 shift, float limit)
    {
        if (shift.X == 0 || shift.Y == 0)
        {
            return shift.Clamp(Vector2.One * -limit, Vector2.One * limit);
        }

        shift *= Math.Clamp(shift.X, -limit, limit) / shift.X;
        shift *= Math.Clamp(shift.Y, -limit, limit) / shift.Y;

        return shift;
    }

    private static float sigmoid(float value) => 1 / (1 + Mathf.Exp(-value));

    private static float hitboxSize() => (float)(0.5 + Constants.HIT_BOX_SIZE);

    private static int lowerBound(List<AutoNote> notes, double time)
    {
        int low = 0;
        int high = notes.Count;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (notes[middle].Millisecond < time)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    private static List<AutoNote> mergeSimultaneousNotes(List<AutoNote> notes)
    {
        for (int i = 1; i < notes.Count; i++)
        {
            if (notes[i].Millisecond < notes[i - 1].Millisecond)
            {
                notes.Sort((a, b) => a.Millisecond.CompareTo(b.Millisecond));
                break;
            }
        }

        List<AutoNote> merged = new(notes.Count);
        int noteIndex = 0;
        while (noteIndex < notes.Count)
        {
            AutoNote first = notes[noteIndex++];
            Vector2 position = first.Position;
            int count = 1;
            while (noteIndex < notes.Count && notes[noteIndex].Millisecond == first.Millisecond)
            {
                position += notes[noteIndex++].Position;
                count++;
            }

            merged.Add(first.WithPosition(position / count));
        }

        return merged;
    }

    private static string getCachePath(List<AutoNote> notes)
    {
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);
        writer.Write(cache_version);
        writer.Write(Constants.HIT_BOX_SIZE);
        writer.Write(Constants.BOUNDS.X);
        writer.Write(Constants.BOUNDS.Y);
        foreach (AutoNote note in notes)
        {
            writer.Write(note.X);
            writer.Write(note.Y);
            writer.Write(note.Millisecond);
        }

        string hash = Convert.ToHexString(SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)));
        return Path.Combine(Constants.USER_FOLDER, "cache", "autoplay", $"{hash}.bin");
    }

    private bool tryLoadCache(string path, int noteCount)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != cache_version)
                return false;

            int count = reader.ReadInt32();
            if (count <= 0 || count > noteCount || stream.Length != 8L + count * 16L)
                return false;

            List<AutoNote> cached = new(count);
            double previousTime = double.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                double time = reader.ReadDouble();
                if (!float.IsFinite(x) || !float.IsFinite(y) || !double.IsFinite(time) || time <= previousTime)
                    return false;

                cached.Add(new(x, y, time));
                previousTime = time;
            }

            processedData.AddRange(cached);
            return true;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return false;
    }

    private void saveCache(string path)
    {
        string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var writer = new BinaryWriter(File.Create(temporaryPath)))
            {
                writer.Write(cache_version);
                writer.Write(processedData.Count);
                foreach (AutoNote note in processedData)
                {
                    writer.Write(note.X);
                    writer.Write(note.Y);
                    writer.Write(note.Millisecond);
                }
            }

            File.Move(temporaryPath, path, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
