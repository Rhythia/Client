using System;
using System.Collections.Generic;
using Godot;

public class Note : IHitObject, IAnimatableObject<NoteAnimation>, IComparable<Note>
{
    public int Id => (int)ObjectType.Note;

    public int ObjectID { get; } = 0;     // map object type id

    public int Index;                  // note index within the map

    public int Millisecond { get; set; }

    public Tween CurrentTween { get; set; }

    public List<NoteAnimation> AnimationObjects { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Transparency { get; set; } = 1;

    public bool Hit { get; set; } = false;

    public bool Hittable { get; set; } = false;

    public Note(int index, int millisecond, float x, float y)
    {
        Index = index;
        Millisecond = millisecond;
        X = x;
        Y = y;
    }

    public int CompareTo(Note other)
    {
        return Millisecond.CompareTo(other.Millisecond);
    }

    int IComparable<ITimelineObject>.CompareTo(ITimelineObject other)
    {
        return Millisecond.CompareTo(other.Millisecond);
    }
}
