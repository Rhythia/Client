using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class HealthJudgment
{
    public bool IsFailed { get; private set; }

    public event Action<bool> Failed;

    private List<IFailModifier> failModifiers = new();

    private List<IHealthModifier> healthModifiers = new();

    public double Health { get; private set; } = 1;

    public double HealthStep { get; private set; } = 15;

    public void ApplyAttempt(Attempt attempt)
    {
        foreach (Mod mod in attempt.Mods)
        {
            if (mod is IHealthModifier healthMod)
            {
                healthModifiers.Add(healthMod);
            }

            if (mod is IFailModifier failMod)
            {
                failModifiers.Add(failMod);
            }
        }
    }

    public void ApplyHitObjectResult(bool hit)
    {
        if (healthModifiers.Any())
        {
            foreach (IHealthModifier mod in healthModifiers)
            {
                Health = mod.ApplyHealthResult(hit, Health);
            }
        }
        else
        {
            defaultHealthResult(hit);
        }

        if (failModifiers.Any())
        {
            foreach (IFailModifier mod in failModifiers)
            {
                if (mod.CheckFailCondition(hit, Health))
                {
                    applyFail();
                    break;
                }
            }
        }
        else
        {
            if (defaultFailResult())
            {
                applyFail();
            }
        }
    }

    private void defaultHealthResult(bool hit)
    {
        if (hit)
        {
            HealthStep = Math.Max(HealthStep / 1.45, 15);
            Health = Math.Min(100, Health + HealthStep / 1.75);
        }
        else
        {
            Health = Math.Max(0, Health - HealthStep);
            HealthStep = Math.Min(HealthStep * 1.2, 100);
        }
    }

    private bool defaultFailResult() => Health > 0;

    private void applyFail()
    {
        IsFailed = true;
        Failed.Invoke(true);
    }
}
