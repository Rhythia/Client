using System;
using System.Collections.Generic;
using Godot;

// this isn't actually a renderer
public partial class TrailRenderer : Node
{
    [Export] private MeshInstance3D cursor;
    [Export] private MultiMeshInstance3D cursorTrail;
    [Export] private Runner runner;

    private const float trail_spawn_rate = 240;
    private const float trail_min_detail = 0;
    private const float trail_max_detail = 100f;

    private List<CursorTrailData> activeTrailsData = [];
    private double deltaAccumulator;

    public override void _ExitTree()
    {
        // cleanup so meshes don't stay after the scene reloads
        activeTrailsData.Clear();
        cursorTrail.Multimesh.InstanceCount = 0;
    }

    public override void _Process(double delta)
    {
        if (!runner.Attempt.Settings.CursorTrail || !runner.Playing) return;
        updateCursorTrail(delta);
    }

    private void updateCursorTrail(double delta)
    {
        ulong now = Time.GetTicksUsec();

        processTrailSpawning(delta, now);
        cullExpiredTrails(now);
        updateTrailRendering(now);
    }

    private void processTrailSpawning(double delta, ulong now)
    {
        float trailDetail = (float)Mathf.Clamp(SettingsManager.Instance.Settings.TrailDetail.Value, trail_min_detail, trail_max_detail);
        float wantedEmission = trailDetail / trail_max_detail;

        float rate = wantedEmission * trail_spawn_rate;
        if (rate <= 0f)
            return;

        double interval = 1.0 / rate;
        deltaAccumulator += delta;

        int steps = (int)(deltaAccumulator / interval);
        if (steps <= 0)
            return;

        activeTrailsData.Add(new CursorTrailData(
            time: now,
            position: runner.Attempt.CursorPosition,
            rotation: cursor.Rotation.Z
        ));
        deltaAccumulator -= interval * steps;
    }

    private void cullExpiredTrails(ulong now)
    {
        ulong maxLifeTime = (ulong)(SettingsManager.Instance.Settings.TrailTime.Value * 1_000_000);
        for (int i = activeTrailsData.Count - 1; i >= 0; i--)
        {
            CursorTrailData trail = activeTrailsData[i];
            ulong age = now - trail.Time;

            if (age >= maxLifeTime)
            {
                activeTrailsData.RemoveAt(i);
            }
        }
    }

    private void updateTrailRendering(ulong now)
    {
        float size = ((Vector2)cursor.Mesh.Get("size")).X;
        cursorTrail.Multimesh.InstanceCount = activeTrailsData.Count;

        for (int j = 0; j < activeTrailsData.Count; j++)
        {
            CursorTrailData trail = activeTrailsData[j];

            Transform3D transform = Transform3D.Identity
                .Scaled(new Vector3(size, size, size))
                .Rotated(Vector3.Back, trail.Rotation);
            transform.Origin = new Vector3(trail.Position.X, trail.Position.Y, 0);

            // skip actually rendering the mesh if the player doesn't move the cursor
            bool skipRender = trail.Position.DistanceTo(runner.Attempt.CursorPosition) == 0;
            if (skipRender)
            {
                cursorTrail.Multimesh.SetInstanceTransform(j, transform);
                cursorTrail.Multimesh.SetInstanceColor(j, new Color(1, 1, 1, 0));
                continue;
            }

            // calculate trail's transparency
            //1. find total time that trail exists
            //2. find amount of steps till it fades
            //3. lerp from 1 (fully opaque) to 0 (fully transparent) with interpolated steps
            float elapsed = (now - trail.Time) / 1_000_000f;
            float step = (float)Math.Clamp(elapsed / SettingsManager.Instance.Settings.TrailTime.Value, 0f, 1f);
            float alpha = Mathf.Lerp(1, 0, step);
            cursorTrail.Multimesh.SetInstanceTransform(j, transform);
            cursorTrail.Multimesh.SetInstanceColor(j, new Color(1, 1, 1, alpha));
        }
    }
}
