using System;
using System.Linq;
using Godot;

public partial class LegacyRenderer : MultiMeshInstance3D
{
    [Export] public Runner Runner;
    private SettingsProfile settings;

    public override void _Ready()
    {
        Runner ??= GetParent<Runner>();
        settings = SettingsManager.Instance.Settings;
    }

    public override void _Process(double delta)
    {
        if (!Runner.Playing)
        {
            return;
        }

        Multimesh.InstanceCount = Runner.ToProcess;

        float ar = (float)(Runner.Attempt.IsReplay ? Runner.Attempt.Replays[0].ApproachRate : settings.ApproachRate.Value);
        float ad = (float)(Runner.Attempt.IsReplay ? Runner.Attempt.Replays[0].ApproachDistance : settings.ApproachDistance.Value);
        float at = ad / ar;
        float fadeIn = (float)(Runner.Attempt.IsReplay ? Runner.Attempt.Replays[0].FadeIn : settings.FadeIn.Value);
        bool fadeOut = (float)Runner.Attempt.IsReplay ? (Runner.Attempt.Replays[0].FadeOut ? 100 : 0) : settings.FadeOut.Value;
        bool pushback = Runner.Attempt.IsReplay ? Runner.Attempt.Replays[0].Pushback : settings.Pushback.Value;
        float hitWindowDepth = pushback ? (float)Constants.HIT_WINDOW * ar / 1000 : 0;
        float noteOpacity = (float)settings.NoteOpacity;

        if (noteOpacity > 1)
        {
            // Backward compatibility: older profiles may have stored opacity in a 0-100 range.
            noteOpacity /= 100f;
        }
        
        noteOpacity = Math.Clamp(noteOpacity, 0, 1);
        
        float noteSize = (float)(Runner.Attempt.IsReplay ? Runner.Attempt.Replays[0].NoteSize : settings.NoteSize.Value) / 4;
        Transform3D transform = new(Vector3.Right * noteSize, Vector3.Up * noteSize, Vector3.Back * noteSize, Vector3.Zero);

        for (int i = 0; i < Runner.ToProcess; i++)
        {
            Note note = Runner.ProcessNotes[i];
            float depth = (note.Millisecond - (float)Runner.Attempt.Progress) / (1000 * at) * ad / (float)Runner.Attempt.Speed;
            float alpha = Math.Clamp((1 - (float)depth / ad) / (fadeIn / 100), 0, 1);

            if (Runner.Attempt.Mods["Ghost"])
            {
                alpha -= (ad - depth) / (ad / 2);
            }
            else if (fadeOut > 0)
            {
                float fadeOutScale = fadeOut / 100f;
                alpha *= Math.Min(1, (depth + hitWindowDepth) / (ad * fadeOutScale + hitWindowDepth));
            }

            if (!pushback && note.Millisecond - Runner.Attempt.Progress <= 0)
            {
                alpha = 0;
            }

            int j = Runner.ToProcess - i - 1;
            Color color = SkinManager.Instance.Skin.NoteColors[note.Index % SkinManager.Instance.Skin.NoteColors.Length];

            transform.Origin = new Vector3(note.X, note.Y, -depth);
            color.A = Math.Clamp(alpha * noteOpacity, 0, 1);
            Multimesh.SetInstanceTransform(j, transform);
            Multimesh.SetInstanceColor(j, color);
        }
    }
}
