using System;
using Godot;

/// <summary>
/// Modifies the timeline object rendering
/// </summary>
public interface IObjectRenderModifier<T> : IMod
    where T : ITimelineObject
{
    /// <summary>
    /// Modifies the rendering of <see cref="ITimelineObject"/> for the <see cref="IObjectRenderModifier{T}"/>
    /// </summary>
    void ModifyRenderObject(T obj, float depth, Attempt attempt);
}
