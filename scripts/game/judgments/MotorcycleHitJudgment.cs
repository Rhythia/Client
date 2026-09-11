using System;
using System.Collections.Generic;

// Motorcycle mode's hit detection: a gate (an old map's Note, reused as-is)
// is cleared if the bike is in the matching lane when the gate's timestamp
// reaches the bike. This is deliberately basic for now - it does not yet
// distinguish a clean hit from a miss, it just resolves the gate either way
// once its hit window has passed, matching Rhythia's HIT_WINDOW.
public class MotorcycleHitJudgment
{
    public void ProcessGates(Attempt attempt, IList<Note> gates)
    {
        foreach (Note gate in gates)
        {
            if (gate.Hit)
            {
                continue;
            }

            double msUntilHit = gate.Millisecond - attempt.Progress;

            if (Math.Abs(msUntilHit) <= Constants.HIT_WINDOW && MotorcycleLanes.LaneFromNoteX(gate.X) == attempt.BikeLane)
            {
                gate.Hit = true;
            }
            else if (msUntilHit < -Constants.HIT_WINDOW)
            {
                gate.Hit = true; // missed - resolved so it stops being checked/rendered
            }
        }
    }
}
