using System;
using Godot;

/// <summary>
/// Base class for modifiers
/// </summary>
public abstract class Mod : IMod
{
    public abstract string Name { get; }

    /// <summary>
    /// Determines if the <see cref="Mod"/> is rankable
    /// </summary>
    public virtual bool Rankable { get; } = false;

    /// <summary>
    /// Score multiplier for the <see cref="Mod"/>
    /// </summary>
    public virtual double ScoreMultiplier { get; } = 1;

    /// <summary>
    /// Mods that are incompatible with the <see cref="Mod"/>
    /// </summary>
    public virtual Type[] IncomptabileMods => [];
}
