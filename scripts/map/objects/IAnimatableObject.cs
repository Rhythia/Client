using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Game objects that can be tweened
/// </summary>
public interface IAnimatableObject<T>
    where T : AnimationObject
{
    Tween CurrentTween { get; }

    List<T> AnimationObjects { get; }
}
