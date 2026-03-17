using System;
using System.Collections.Generic;
using Godot;

public partial class AudioSpectrum : Panel
{
    [ExportGroup("Sampling")]

    [Export]
    public int Bus { get; set => SpectrumAnalyzer = (AudioEffectSpectrumAnalyzerInstance)AudioServer.GetBusEffectInstance(Math.Clamp(value, 0, AudioServer.BusCount), 0); } = 0;

    [Export]
    public int MinFreq = 20;

    [Export]
    public int MaxFreq = 20000;

    [Export]
    public bool NormalizeMagnitude = true;

    [Export(PropertyHint.Range, "0,1")]
    public float MagnitudeCeiling = 0.2f;

    [ExportGroup("Visualizer")]

    [Export]
    public Color BarColor = Color.Color8(255, 255, 255);

    [Export]
    public float BarSize = 4;

    [Export]
    public float BarGap = 4;

    [Export]
    public float BarMinSize = 0;

    [Export(PropertyHint.Range, "0,1")]
    public float Align = 0.5f;

    [Export(PropertyHint.Range, "0,100")]
    public float Responsiveness = 24;

    public AudioEffectSpectrumAnalyzerInstance SpectrumAnalyzer;

    private int barCount = 0;

    private float ceiling = 0.01f;

    private float targetCeiling = 0.01f;

    private float[] magnitudes = [];

    public override void _Ready()
    {
        SpectrumAnalyzer = (AudioEffectSpectrumAnalyzerInstance)AudioServer.GetBusEffectInstance(Bus, 0);
    }

    public override void _Process(double delta)
    {
        barCount = (int)Math.Round((Size.X - BarSize) / (BarSize + BarGap));

        if (magnitudes.Length != barCount)
        {
            magnitudes = new float[barCount];
        }

        float freqStep = (MaxFreq - MinFreq) / (float)barCount;
        float maxMagnitude = 0;

        for (int i = 0; i < barCount; i++)
        {
            float freqLower = MinFreq + Math.Max(0, i * freqStep);
            float freqUpper = freqLower + freqStep;
            float magnitude = SpectrumAnalyzer.GetMagnitudeForFrequencyRange(freqLower, freqUpper).Length();

            magnitudes[i] = Mathf.Lerp(magnitudes[i], magnitude, (float)Math.Min(1, delta * Responsiveness));

            if (magnitudes[i] > maxMagnitude)
            {
                maxMagnitude = magnitudes[i];
            }
        }

        targetCeiling = NormalizeMagnitude ? (maxMagnitude > 0.0015 ? maxMagnitude : targetCeiling) : MagnitudeCeiling;
        ceiling = Mathf.Lerp(ceiling, targetCeiling, (float)Math.Min(1, delta * 6));

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (barCount == 0) { return; }

        Vector2 size = new(Size.X - BarSize, Size.Y);
        Vector2[] points = new Vector2[barCount * 2];

        for (int i = 0; i < barCount; i++)
        {
            float scale = Math.Clamp(magnitudes[i] / ceiling, BarMinSize / size.Y, 1);
            Vector2 start = new(i / (barCount - 1f) * size.X + BarSize / 2, Align * (1 - scale) * size.Y);
            Vector2 end = start + Vector2.Down * scale * size.Y;

            points[i * 2] = start;
            points[i * 2 + 1] = end;
        }

        DrawMultiline(points, BarColor, BarSize, true);
    }
}
