using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class Note : IHitObject, IAnimatableObject<NoteAnimation>, IComparable<Note>
{
    public int Index;
    public float X { get; set; }
    public float Y { get; set; }
    public bool Hittable { get; set; } = false;
    public HitResult LastResult { get; set; } = HitResult.None;

    public int Id => (int)ObjectType.Note;
    public int ObjectID { get; } = 0;     // map object type id

    public int Millisecond { get; set; }
    public Tween CurrentTween { get; set; }
    public List<NoteAnimation> AnimationObjects { get; set; }
    public float Transparency { get; set; } = 1;

    public Note(int index, int millisecond, float x, float y)
    {
        Index = index;
        Millisecond = millisecond;
        X = x;
        Y = y;
    }

    public void Hit(Runner runner)
    {
        if (LastResult != HitResult.None) return;
        LastResult = HitResult.Hit;
        runner.EmitSignal(Runner.SignalName.HitResultChanged, Index, (int)LastResult);
        SoundManager.HitSound.Play();
    }

    public void Miss(Runner runner)
    {
        if (LastResult != HitResult.None) return;
        LastResult = HitResult.Miss;
        runner.EmitSignal(Runner.SignalName.HitResultChanged, Index, (int)LastResult);
        SoundManager.MissSound.Play();
    }

    public HitResult CheckHitResult(Attempt attempt)
    {
        //if (!Hittable) return HitResult.None;

        if (attempt.CursorPosition.X + Constants.HIT_BOX_SIZE >= X - 0.5f &&
            attempt.CursorPosition.X - Constants.HIT_BOX_SIZE <= X + 0.5f &&
            attempt.CursorPosition.Y + Constants.HIT_BOX_SIZE >= Y - 0.5f &&
            attempt.CursorPosition.Y - Constants.HIT_BOX_SIZE <= Y + 0.5f)
		{
			return HitResult.Hit;
		}

        return HitResult.Miss;
    }
    
    public int CompareTo(Note other)
    {
        return Millisecond.CompareTo(other.Millisecond);
    }

    public int CompareTo(ITimelineObject other)
    {
        throw new NotImplementedException();
    }
}
