using System;
using Godot;

// Collapses the old 3x3 grid map format down to 3 lanes for motorcycle mode.
// Old maps place notes at X/Y roughly in {-1, 0, 1}; we keep X as the lane
// and ignore Y entirely, so a chart with notes spread across all 3 rows
// still plays back as a single row of 3 lanes.
public static class MotorcycleLanes
{
    public const int LaneCount = 3;

    public static int LaneFromNoteX(float x)
    {
        return Math.Clamp(Mathf.RoundToInt(x), -1, 1);
    }

    public static float LaneWorldX(int lane)
    {
        return lane * Constants.MOTORCYCLE_LANE_WIDTH;
    }
}
