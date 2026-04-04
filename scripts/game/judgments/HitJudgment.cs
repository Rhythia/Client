using System;
using System.Collections.Generic;
using Godot;

public class HitJudgment
{
	private List<IHitObject> _hitObjects = new();
	private List<IHitWindowModifier> _hitWindowModifiers = new();
	private float _activeHitWindow = (float)Constants.HIT_WINDOW;

	public void ApplyAttempt(Attempt attempt)
	{
		// Reset state
		_hitObjects.Clear();
		_hitWindowModifiers.Clear();
		_activeHitWindow = (float)Constants.HIT_WINDOW;

		// Populate hit objects from Map
		if (attempt.Map != null && attempt.Map.Notes != null)
		{
			foreach (var note in attempt.Map.Notes)
			{
				_hitObjects.Add(note);
			}
		}

		// Gather modifiers
		if (attempt.Mods != null)
		{
			foreach (Mod mod in attempt.Mods)
			{
				if (mod is IHitWindowModifier hitWindowMod)
				{
					_hitWindowModifiers.Add(hitWindowMod);
				}
			}
		}

		// Calculate final hit window
		foreach (IHitWindowModifier mod in _hitWindowModifiers)
		{
			_activeHitWindow = mod.ApplyHitWindow(_activeHitWindow);
		}
	}

	public List<JudgmentResult> ProcessHitJudgments(Attempt attempt)
	{
		List<JudgmentResult> results = new();

		foreach (IHitObject note in _hitObjects)
		{
			// Only process notes that are hittable and haven't been hit yet
			if (note.Hittable && !note.Hit)
			{
				float delta = (float)(attempt.Progress - note.Millisecond);

				// Detect missed notes (passed their hitwindow)
				if (delta > _activeHitWindow)
				{
					// Mark as no longer hittable and not hit (Miss) to avoid double processing
					note.Hittable = false;
					note.Hit = false;

					results.Add(new JudgmentResult(note, false, delta));
				}
			}
		}

		return results;
	}
}
