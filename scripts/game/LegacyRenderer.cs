using System;
using Godot;

public partial class LegacyRenderer : MultiMeshInstance3D
{
    private SettingsProfile settings;

    public override void _Ready()
    {
        settings = SettingsManager.Instance.Settings;
    }

    public override void _Process(double delta)
    {
        if (!LegacyRunner.Playing || LegacyRunner.CurrentAttempt.Stopped)
        {
            return;
        }

        Multimesh.InstanceCount = LegacyRunner.ToProcess;

        if (LegacyRunner.ToProcess == 0)
        {
            return;
        }

        float ar = (float)(LegacyRunner.CurrentAttempt.IsReplay ? LegacyRunner.CurrentAttempt.Replays[0].ApproachRate : settings.ApproachRate.Value);
        float ad = (float)(LegacyRunner.CurrentAttempt.IsReplay ? LegacyRunner.CurrentAttempt.Replays[0].ApproachDistance : settings.ApproachDistance.Value);
        float at = ad / ar;
        float fadeIn = (float)(LegacyRunner.CurrentAttempt.IsReplay ? LegacyRunner.CurrentAttempt.Replays[0].FadeIn : settings.FadeIn.Value) / 100;
        float fadeOut = (float)(LegacyRunner.CurrentAttempt.IsReplay ? (LegacyRunner.CurrentAttempt.Replays[0].FadeOut ? 100 : 0) : settings.FadeOut.Value) / 100;
        bool pushback = LegacyRunner.CurrentAttempt.IsReplay ? LegacyRunner.CurrentAttempt.Replays[0].Pushback : settings.Pushback.Value;
        float hitWindowDepth = pushback ? (float)Constants.HIT_WINDOW * ar / 1000 : 0;
        float noteOpacity = (float)settings.NoteOpacity;

        if (noteOpacity > 1)
        {
            // Backward compatibility: older profiles may have stored opacity in a 0-100 range.
            noteOpacity /= 100f;
        }

        noteOpacity = Math.Clamp(noteOpacity, 0, 1);

        float noteSize = (float)(LegacyRunner.CurrentAttempt.IsReplay ? LegacyRunner.CurrentAttempt.Replays[0].NoteSize : settings.NoteSize.Value) / 2;
        Transform3D transform = new(Vector3.Right * noteSize, Vector3.Up * noteSize, Vector3.Back * noteSize, Vector3.Zero);

        for (int i = 0; i < LegacyRunner.ToProcess; i++)
        {
            Note note = LegacyRunner.ProcessNotes[i];
            float depth = (note.Millisecond - (float)LegacyRunner.CurrentAttempt.Progress) / (1000 * at) * ad / (float)LegacyRunner.CurrentAttempt.Speed;
            float progress = 1 - Math.Max(0, (depth + hitWindowDepth) / (ad + hitWindowDepth));
            float alpha = 1;

            if (fadeIn > 0)
            {
                alpha = Math.Min(1, progress / fadeIn);
            }

            if (LegacyRunner.CurrentAttempt.Mods["Ghost"])
            {
                alpha -= (ad - depth) / (ad / 2);
            }
            else if (fadeOut > 0)
            {
                alpha -= 1 - Math.Min(1, (1 - progress) / fadeOut);
            }

            if (!pushback && note.Millisecond - LegacyRunner.CurrentAttempt.Progress <= 0)
            {
                alpha = 0;
            }

            int j = LegacyRunner.ToProcess - i - 1;
            Color color = SkinManager.Instance.Skin.NoteColors[note.Index % SkinManager.Instance.Skin.NoteColors.Length];

            transform.Origin = new Vector3(note.X, note.Y, -depth);
            color.A = Math.Clamp(alpha * noteOpacity, 0, 1);
            Multimesh.SetInstanceTransform(j, transform);
            Multimesh.SetInstanceColor(j, color);
        }
    }
}
