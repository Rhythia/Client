using System;
using System.Collections.Generic;
using Godot;

public partial class Note : IHitObject, IAnimatableObject<NoteAnimation>, IComparable<Note>
{
    public int Id => (int)ObjectType.Note;
    public int Index { get; set; } = -1;
    public int Millisecond { get; set; }

    public float X { get; set; }
    public float Y { get; set; }
    public int HitWindow { get; set; } = 55;
    public bool Hittable { get; set; } = false;
    public HitResult LastResult { get; set; } = HitResult.None;

    public Tween CurrentTween { get; set; }
    public List<NoteAnimation> AnimationObjects { get; set; }
    public float Opacity { get; set; } = 1;
    public Transform3D Transform = Transform3D.Identity;

    public Note(int index, int millisecond, float x, float y)
    {
        Index = index;
        Millisecond = millisecond;
        X = x;
        Y = y;
    }

    public void Hit(Runner runner, bool playSound = true)
    {
        if (LastResult != HitResult.None) return;

        LastResult = HitResult.Hit;
        runner.EmitSignal(Runner.SignalName.HitResultChanged, Index, (int)LastResult);

        if (playSound) SoundManager.PlayHitSound();
    }

    public void Miss(Runner runner, bool playSound = true)
    {
        if (LastResult != HitResult.None) return;

        LastResult = HitResult.Miss;
        runner.EmitSignal(Runner.SignalName.HitResultChanged, Index, (int)LastResult);

        if (playSound) SoundManager.PlayMissSound();
    }

    public bool DoProcess(Runner runner)
    {
        var attempt = runner.Attempt;

        return Millisecond >= attempt.StartFrom
            && Millisecond - attempt.Progress <= 0
            && LastResult == HitResult.None;
    }

    public void Process(Runner runner)
    {
        var attempt = runner.Attempt;

        if (!Hittable)
        {
            Hittable = true;

            if (attempt.Settings.AlwaysPlayHitSound)
            {
                SoundManager.PlayHitSound();
            }
        }

        bool late = isPastWindow(runner, HitWindow);
        float replayLateness = attempt.IsReplay && attempt.Replays.Length == 1 ? attempt.Replays[0].Notes[Index] : 0;

        if (late && (!attempt.IsReplay || replayLateness == -1))
        {
            Miss(runner);
        }
        else if (!attempt.IsReplay && CheckHitResult(attempt) == HitResult.Hit
            || attempt.IsReplay && replayLateness != -1 && isPastWindow(runner, replayLateness))
        {
            Hit(runner);
        }
    }

    public HitResult CheckHitResult(Attempt attempt)
    {
        if (attempt.CursorPosition.X + Constants.HIT_BOX_SIZE >= X - 0.5f
            && attempt.CursorPosition.X - Constants.HIT_BOX_SIZE <= X + 0.5f
            && attempt.CursorPosition.Y + Constants.HIT_BOX_SIZE >= Y - 0.5f
            && attempt.CursorPosition.Y - Constants.HIT_BOX_SIZE <= Y + 0.5f)
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

    private bool isPastWindow(Runner runner, float hitWindow)
    {
        return Millisecond < runner.Attempt.Progress - hitWindow * runner.Speed;
    }
}
