using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class NoteRenderer : Renderer, IRenderer<Note>
{
    [Export] private Runner runner;

    public MultiMeshInstance3D NoteMultiMesh { get; set; }

    private Color transparent = new(0xffffff00);

    public override void _Ready()
    {
        runner ??= GetParent().GetParent<Runner>();

        NoteMultiMesh = new()
        {
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Multimesh = new()
            {
                UseColors = true,
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D
            }
        };

        AddChild(NoteMultiMesh);
    }

    public override void Setup(SettingsProfile settings, SkinProfile skin)
    {
        base.Setup(settings, skin);

        NoteMultiMesh.Multimesh.InstanceCount = 0;
        NoteMultiMesh.Multimesh.VisibleInstanceCount = -1;

        var mesh = NoteMultiMesh.Multimesh.Mesh;

        for (int i = 0; i < mesh.GetSurfaceCount(); i++)
        {
            if (mesh.SurfaceGetMaterial(i) is not StandardMaterial3D mat) continue;

            mat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
            mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            mat.DisableFog = true;
            mat.DisableAmbientLight = true;
            mat.DisableSpecularOcclusion = true;
            mat.VertexColorUseAsAlbedo = true;
            mat.VertexColorIsSrgb = true;
        }
    }

    private bool doRender(Note note, float time, float approachTime, float speed)
    {
        return note.Millisecond - time >= (Settings.Pushback ? -Constants.HIT_WINDOW * speed : 0) && note.Millisecond - time <= approachTime * 1000 * speed;
    }

    public void Render(double delta, double time, IList<Note> notes)
    {
        var attempt = runner.Attempt;
        float ar = (float)Settings.ApproachRate;
        float ad = (float)Settings.ApproachDistance;
        float at = (float)Settings.ApproachTime;
        float noteSize = (float)Settings.NoteSize;
        float fadeIn = (float)Settings.FadeIn / 100;
        float fadeOut = (float)Settings.FadeOut / 100;
        float noteOpacity = (float)Settings.NoteOpacity;
        float noteOpacityExponent = Math.Max(Mathf.Epsilon, (float)Settings.NoteOpacityExponent);
        bool pushback = Settings.Pushback;
        float hitWindowDepth = pushback ? (float)Constants.HIT_WINDOW * ar / 1000 : 0;
        var transform = new Transform3D(new(noteSize / 2, 0, 0), new(0, noteSize / 2, 0), new(0, 0, noteSize / 2), Vector3.Zero);

        if (notes.Count > NoteMultiMesh.Multimesh.InstanceCount)
        {
            NoteMultiMesh.Multimesh.InstanceCount = notes.Count;
            NoteMultiMesh.Multimesh.VisibleInstanceCount = NoteMultiMesh.Multimesh.InstanceCount;
        }
        else
        {
            NoteMultiMesh.Multimesh.VisibleInstanceCount = notes.Count;
        }

        for (int i = 0; i < notes.Count; i++)
        {
            var note = notes[i];

            if (!doRender(note, (float)time, at, (float)attempt.Speed) || note.LastResult == HitResult.Hit)
            {
                NoteMultiMesh.Multimesh.SetInstanceColor(i, transparent);
                continue;
            }

            float depth = (note.Millisecond - (float)attempt.Progress) / (1000 * at) * ad / (float)attempt.Speed;
            float progress = 1 - Math.Max(0, (depth + hitWindowDepth) / (ad + hitWindowDepth));

            note.Opacity = 1;
            note.Transform = transform;

            if (fadeIn > 0)
            {
                note.Opacity = Math.Min(1, progress / fadeIn);
            }

            if (fadeOut > 0 && attempt.UseFadeOut)
            {
                note.Opacity -= 1 - Math.Min(1, (1 - progress) / fadeOut);
            }

            note.Transform.Origin = new(note.X, note.Y, -depth);

            foreach (var mod in attempt.Modifiers)
            {
                if (mod is IObjectRenderModifier<Note> modifier)
                {
                    modifier.ModifyRenderObject(note, attempt);
                }
            }

            var color = SkinManager.Instance.Skin.NoteColors[note.Index % SkinManager.Instance.Skin.NoteColors.Length];

            color.A = Math.Clamp((float)Math.Pow(Math.Max(0, note.Opacity * noteOpacity), noteOpacityExponent), 0, 1);
            NoteMultiMesh.Multimesh.SetInstanceTransform(i, note.Transform);
            NoteMultiMesh.Multimesh.SetInstanceColor(i, color);
        }
    }

    public override void Process(double delta, Attempt attempt)
    {
        var type = typeof(Note);

        if (!attempt.Objects.TryGetValue(type, out var notes))
        {
            return;
        }

        int start = runner.ObjectIndicesStart[type];
        int end = runner.ObjectIndicesEnd[type];
        notes = notes[start..end];

        Render(delta, attempt.Progress, [.. notes.Cast<Note>()]);
    }
}
