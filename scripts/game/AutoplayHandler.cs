using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class AutoplayHandler
{
    private readonly Attempt attempt;
    private readonly List<AutoNote> processedData = [];
    private int lastLoadedNote;

    private const float max_shift_multiplier = 0.25f;

    private readonly struct AutoNote(float x, float y, double millisecond)
    {
        public readonly float X = x;
        public readonly float Y = y;
        public readonly double Millisecond = millisecond;

        public Vector2 Position => new(X, Y);

        public AutoNote WithPosition(Vector2 position) => new(position.X, position.Y, Millisecond);

        public AutoNote WithMillisecond(double millisecond) => new(X, Y, millisecond);
    }

    public AutoplayHandler(Attempt attempt)
    {
        this.attempt = attempt;

        var notes = attempt.Map.Notes
            .Select(note => new AutoNote(note.X, note.Y, note.Millisecond))
            .ToList();

        if (notes.Count == 0)
        {
            return;
        }

        // this can be used for difficulty calculation
        List<AutoNote> preprocessedData = initialPreprocess(notes);
        List<AutoNote> secondaryPreprocessedData = compressStacks(preprocessedData);
        shiftPreprocess(notes, preprocessedData, secondaryPreprocessedData);
    }

    public void Reset(double progress)
    {
        lastLoadedNote = 0;

        while (lastLoadedNote + 1 < processedData.Count && processedData[lastLoadedNote + 1].Millisecond <= progress)
        {
            lastLoadedNote++;
        }
    }

    public Vector2 GetCursorPosition(double progress)
    {
        if (processedData.Count == 0)
        {
            return Vector2.Zero;
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

            if (i > 1)
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

    private List<AutoNote> compressStacks(List<AutoNote> preprocessedData)
    {
        if (preprocessedData.Count <= 5)
        {
            return [.. preprocessedData];
        }

        List<AutoNote> secondaryPreprocessedData = [preprocessedData[0], preprocessedData[1]];
        int i = 2;

        while (i + 3 < preprocessedData.Count)
        {
            int stackLength = 1;
            AutoNote topNote = preprocessedData[i];
            int topNoteIndex = i;
            i++;

            while (i + 2 < preprocessedData.Count)
            {
                AutoNote nextNote = preprocessedData[i];
                i++;

                if (checkHit(nextNote, topNote, 0.1f))
                {
                    stackLength++;
                }
                else
                {
                    break;
                }
            }

            i--;

            if (stackLength > 1)
            {
                AutoNote endNote = preprocessedData[i - 1];
                bool valid = false;
                AutoNote validTest = topNote;
                List<AutoNote> tests = buildStackTests(topNote, endNote);
                int checkStart = Math.Max(0, topNoteIndex - 6);
                List<AutoNote> notesToCheck = preprocessedData.GetRange(checkStart, Math.Min(i + 6, preprocessedData.Count) - checkStart);
                List<AutoNote> cursorPositionNotes = secondaryPreprocessedData.GetRange(Math.Max(0, secondaryPreprocessedData.Count - stackLength - 12), Math.Min(stackLength + 12, secondaryPreprocessedData.Count));
                List<AutoNote> cursorPositionNotesEnd = preprocessedData.GetRange(i, Math.Min(12, preprocessedData.Count - i));

                foreach (AutoNote test in tests)
                {
                    bool currentValid = true;
                    List<AutoNote> validation = [.. cursorPositionNotes, test, .. cursorPositionNotesEnd];

                    foreach (AutoNote note in notesToCheck)
                    {
                        bool anyValid = false;

                        for (int offsetTest = 0; offsetTest < 10; offsetTest++)
                        {
                            float offsetCheck = Mathf.Lerp(5, (float)Constants.HIT_WINDOW * 0.8f, 1 - offsetTest / 9.0f);

                            if (checkHit(note, getCursorPositionFromNotes(validation, note.Millisecond + offsetCheck), hitboxSize() * 0.74f))
                            {
                                anyValid = true;
                                break;
                            }
                        }

                        if (!anyValid)
                        {
                            currentValid = false;
                            break;
                        }
                    }

                    if (currentValid)
                    {
                        valid = true;
                        validTest = test;
                        break;
                    }
                }

                if (valid)
                {
                    secondaryPreprocessedData.Add(validTest);
                }
                else
                {
                    secondaryPreprocessedData.Add(topNote);
                    secondaryPreprocessedData.Add(topNote.WithMillisecond(topNote.Millisecond + 10));
                    secondaryPreprocessedData.Add(endNote.WithMillisecond(endNote.Millisecond - 10));
                    secondaryPreprocessedData.Add(endNote);
                }
            }
            else
            {
                secondaryPreprocessedData.Add(topNote);
            }
        }

        secondaryPreprocessedData.Add(preprocessedData[^3]);
        secondaryPreprocessedData.Add(preprocessedData[^2]);
        secondaryPreprocessedData.Add(preprocessedData[^1]);

        return secondaryPreprocessedData;
    }

    private List<AutoNote> buildStackTests(AutoNote topNote, AutoNote endNote)
    {
        List<AutoNote> tests = [];
        float testWidth = hitboxSize() * 0.5f;
        const int test_width_fidelity = 3;
        const int test_count = 11;

        for (int i = 0; i < test_count; i++)
        {
            double millisecond = Mathf.Lerp(topNote.Millisecond, endNote.Millisecond, i / (test_count - 1.0f));
            tests.Add(new(topNote.X, topNote.Y, millisecond));

            for (int x = -test_width_fidelity; x <= test_width_fidelity; x++)
            {
                for (int y = -test_width_fidelity; y <= test_width_fidelity; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    Vector2 offset = new Vector2(x, y) / test_width_fidelity * testWidth;
                    tests.Add(new(topNote.X + offset.X, topNote.Y + offset.Y, millisecond));
                }
            }
        }

        Vector2 topNotePos = topNote.Position;
        tests.Sort((a, b) =>
        {
            int timeCompare = a.Millisecond.CompareTo(b.Millisecond);
            return timeCompare != 0 ? timeCompare : a.Position.DistanceSquaredTo(topNotePos).CompareTo(b.Position.DistanceSquaredTo(topNotePos));
        });

        return tests;
    }

    private void shiftPreprocess(List<AutoNote> originalNotes, List<AutoNote> preprocessedData, List<AutoNote> secondaryPreprocessedData)
    {
        float maxRange = hitboxSize() * max_shift_multiplier;

        for (int i = 0; i + 1 < secondaryPreprocessedData.Count; i++)
        {
            // this can be used for difficulty calculation
            AutoNote note0 = secondaryPreprocessedData[Math.Max(i - 2, 0)];
            AutoNote note1 = secondaryPreprocessedData[Math.Max(i - 1, 0)];
            AutoNote note2 = secondaryPreprocessedData[i];
            AutoNote note3 = secondaryPreprocessedData[i + 1];
            AutoNote note4 = secondaryPreprocessedData[Math.Min(i + 2, secondaryPreprocessedData.Count - 1)];

            Vector2 shiftVec;

            if (note2.X == note3.X && note2.Y == note3.Y)
            {
                Vector2 previous = processedData.Count > 0 ? processedData[^1].Position : note1.Position;
                Vector2 desired = (previous + note2.Position * 0.5f + note3.Position) / 2.5f;
                shiftVec = clampShift(desired - note2.Position, hitboxSize() * 0.75f);
            }
            else
            {
                Vector2 pos = getSplinePosition(note0, note1, note3, note4, note2.Millisecond);
                shiftVec = clampShift(pos - note2.Position, maxRange);
            }

            bool valid = false;

            for (int testIndex = 0; testIndex < 1; testIndex++)
            {
                float shiftMulti = (10 - testIndex) / 10.0f;
                AutoNote newNote = note2.WithPosition(note2.Position + shiftVec * shiftMulti);
                List<AutoNote> validation = [
                    .. processedData.TakeLast(3),
                    newNote,
                    .. secondaryPreprocessedData.Skip(i + 1).Take(3)
                ];

                if (validation.Count < 4)
                {
                    processedData.Add(newNote);
                    valid = true;
                    break;
                }

                bool currentValid = true;
                double low = validation[Math.Min(2, validation.Count - 1)].Millisecond;
                double high = validation[Math.Max(0, validation.Count - 3)].Millisecond;

                foreach (AutoNote note in originalNotes)
                {
                    if (note.Millisecond >= low && note.Millisecond <= high)
                    {
                        bool hit = checkHit(note, getCursorPositionFromNotes(validation, note.Millisecond + 1), hitboxSize() * 0.9f)
                            || checkHit(note, getCursorPositionFromNotes(validation, note.Millisecond + 5), hitboxSize() * 0.9f);

                        if (!hit)
                        {
                            currentValid = false;
                            break;
                        }
                    }
                }

                if (currentValid)
                {
                    processedData.Add(newNote);
                    valid = true;
                    break;
                }
            }

            if (!valid)
            {
                processedData.Add(note2);
            }
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

        float u = (float)((time - note1.Millisecond) / segmentDuration);

        return new(
            catmullRomRaw(note0.X, note1.X, note2.X, note3.X, u, note0.Millisecond, note1.Millisecond, note2.Millisecond, note3.Millisecond),
            catmullRomRaw(note0.Y, note1.Y, note2.Y, note3.Y, u, note0.Millisecond, note1.Millisecond, note2.Millisecond, note3.Millisecond)
        );
    }

    private static float catmullRomRaw(float pos0, float pos1, float pos2, float pos3, float u, double time0, double time1, double time2, double time3)
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

        return (2 * (pos1 - pos2) + m1 + m2) * u2 * u
            + (-3 * (pos1 - pos2) - m1 - m1 - m2) * u2
            + m1 * u
            + pos1;
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
}
