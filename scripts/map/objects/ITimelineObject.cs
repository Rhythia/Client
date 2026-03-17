using System;
using Godot;

/// <summary>
/// Base interface for a map object
/// </summary>
public interface ITimelineObject : IComparable<ITimelineObject>
{
    /// <summary>
    /// Identity of the <see cref="ITimelineObject"/>
    /// <para>Each object type must have a unique ID</para>
    /// </summary>
    int Id { get; }

    ///// <summary>
    ///// Index position of the <see cref="ITimelineObject"/>
    ///// </summary>
    //public int Index { get; }

    /// <summary>
    /// Millisecond timing for the <see cref="ITimelineObject"/>
    /// </summary>
    int Millisecond { get; }
}
