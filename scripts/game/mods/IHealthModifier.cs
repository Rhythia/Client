using System;
using Godot;

public interface IHealthModifier : IMod
{
    double ApplyHealthResult(bool hit, double health);
}
