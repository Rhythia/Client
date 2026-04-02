using System;

public struct JudgmentResult
{
    /// <summary>
    /// The hit object that was judged.
    /// </summary>
    public IHitObject Note;

    /// <summary>
    /// Whether the note was hit (true) or missed (false).
    /// </summary>
    public bool Hit;

    /// <summary>
    /// The timing offset in milliseconds (positive = late, negative = early).
    /// For misses, this is typically equal to or greater than the hit window.
    /// </summary>
    public float Lateness;

    public JudgmentResult(IHitObject note, bool hit, float lateness)
    {
        Note = note;
        Hit = hit;
        Lateness = lateness;
    }
}
