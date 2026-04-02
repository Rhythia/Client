using System;

public class HardRockMod : Mod, IHitWindowModifier
{
	public override string Name => "HardRock";

	public override bool Rankable => true;

	public override double ScoreMultiplier => 1.08; // Based on Constants.cs comment

	public float ApplyHitWindow(float currentWindow)
	{
		return currentWindow * 0.5f; // 50% reduction as planned
	}
}
