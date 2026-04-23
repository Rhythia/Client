using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

public partial class Runner : Node3D
{
	[Signal] public delegate void AttemptStatsUpdatedEventHandler(Attempt attempt);
	[Signal] public delegate void SkipAvailableEventHandler(Attempt attempt);
	[Signal] public delegate void HitResultChangedEventHandler(int noteIndex, HitResult hitResult);

	[Export] public HudManager HudManager;

	public Attempt Attempt;

	public Map Map;
	private SettingsProfile settings;
	public double Speed = 1;
	public bool Paused = false;
	public bool Playing = false;
	public bool StopQueued = false;

	public int ToProcess = 0;
	public List<Note> ProcessNotes = [];
	private double lastFrame = Time.GetTicksUsec();

	public bool SpinCamera = false;

	[ExportCategory("Settings")]
	[Export] public bool NotesOnly = false;

	[ExportCategory("Nodes")]
	[Export] public Camera3D Camera;
	[Export] public MeshInstance3D Grid;
	[Export] public MeshInstance3D Cursor;
	[Export] public MultiMeshInstance3D CursorTrail;
	[Export] public MultiMeshInstance3D Notes;
	[Export] public VideoStreamPlayer VideoStreamPlayer;

	private List<Dictionary<string, object>> lastCursorPositions = []; //temp, making class for ts

	public override void _Ready()
	{
		base._Ready();

		// if null, assign them to nodes under Runner
		HudManager ??= GetNode<HudManager>("HUD");
		Camera ??= GetNode<Camera3D>("Camera3D");
		Grid ??= HudManager.GetNode<MeshInstance3D>("Grid");
		Cursor ??= GetNode<MeshInstance3D>("Cursor");
		Notes ??= GetNode<MultiMeshInstance3D>("Notes");
		VideoStreamPlayer ??= GetNode<MeshInstance3D>("Video").GetNode<SubViewport>("VideoViewport").GetNode<VideoStreamPlayer>("VideoStreamPlayer");
	}

	public override void _Process(double delta)
	{
		ulong now = Time.GetTicksUsec();
		delta = (now - lastFrame) / 1000000;    // more reliable
		lastFrame = now;

		if (!Playing) return;
		Attempt.Progress += delta * 1000 * Attempt.Speed;

		// if not paused & record replays on & not a temporary map & time from now and last replay frame was 60 frames apart
		if (!Attempt.Stopped && settings.RecordReplays && !Attempt.Map.Ephemeral && now - Attempt.LastReplayFrame >= 1000000/60)
		{
			if (Attempt.ReplayFrames.Count == 0 || (Attempt.ReplayFrames[^1][1 .. 2] != new float[]{Attempt.CursorPosition.X, Attempt.CursorPosition.Y}))
			{
				Attempt.LastReplayFrame = now;
				Attempt.ReplayFrames.Add([
					(float)Attempt.Progress,
					Attempt.CursorPosition.X,
					Attempt.CursorPosition.Y
				]);
			}
		}

		Cursor.RotationDegrees += Vector3.Back * settings.CursorRotation * (float)delta;

		// i hate this shit
        if (settings.CursorTrail)
        {
            List<Dictionary<string, object>> culledList = [];

            lastCursorPositions.Add(new()
            {
                ["Time"] = now,
                ["Position"] = Attempt.CursorPosition,
                ["Rotation"] = Cursor.Rotation.Z
            });

            foreach (Dictionary<string, object> entry in lastCursorPositions)
            {
                if (now - (ulong)entry["Time"] >= (settings.TrailTime * 1000000))
                {
                    continue;
                }

                if (Attempt.CursorPosition.DistanceTo((Vector2)entry["Position"]) == 0)
                {
                    continue;
                }

                culledList.Add(entry);
            }

            int count = culledList.Count;
            float size = ((Vector2)Cursor.Mesh.Get("size")).X;
            Transform3D transform = new Transform3D(new Vector3(size, 0, 0), new Vector3(0, size, 0), new Vector3(0, 0, size), Vector3.Zero);
            int j = 0;

            CursorTrail.Multimesh.InstanceCount = count;

            foreach (Dictionary<string, object> entry in culledList)
            {
                ulong difference = now - (ulong)entry["Time"];
                uint alpha = (uint)(difference / (settings.TrailTime * 1000000) * 255);

                transform.Origin = new Vector3(((Vector2)entry["Position"]).X, ((Vector2)entry["Position"]).Y, 0);
                transform = transform.RotatedLocal(Vector3.Back, (float)entry["Rotation"]);

                CursorTrail.Multimesh.SetInstanceTransform(j, transform);
                CursorTrail.Multimesh.SetInstanceColor(j, Color.FromHtml($"ffffff{255 - alpha:X2}"));
                j++;
            }
        }
        else
        {
            CursorTrail.Multimesh.InstanceCount = 0;
        }

		if (Attempt.Map.AudioBuffer != null)
		{
			if (Attempt.Progress >= Attempt.MapLength - Constants.HIT_WINDOW)
			{
				if (SoundManager.Song.Playing)
				{
					SoundManager.Song.Stop();
				}
			}
			else if (!SoundManager.Song.Playing && Attempt.Progress >= 0)
			{
				SoundManager.Song.Play();
				SoundManager.Song.Seek((float)Attempt.Progress / 1000);
			}
		}

		int nextNoteMillisecond = Attempt.PassedNotes >= Attempt.Map.Notes.Length ? (int)Attempt.MapLength : Attempt.Map.Notes[Attempt.PassedNotes].Millisecond;
		if (nextNoteMillisecond - Attempt.Progress >= Constants.BREAK_TIME * Attempt.Speed)
		{
			int lastNoteMillisecond = Attempt.PassedNotes > 0 ? Attempt.Map.Notes[Attempt.PassedNotes - 1].Millisecond : 0;
			int skipWindow = nextNoteMillisecond - Constants.BREAK_TIME - lastNoteMillisecond;

			if (skipWindow >= 1000 * Attempt.Speed) // only allow skipping if i'm gonna allow it for at least 1 second
			{
				if (!Attempt.CanSkip)
				{
					Attempt.CanSkip = true;
					EmitSignal(SignalName.SkipAvailable, Attempt);
				}
			}
		}
		else
		{
			Attempt.CanSkip = false;
		}

		ToProcess = 0;
		ProcessNotes.Clear();

		// note process check
		double at = Attempt.IsReplay ? Attempt.Replays[0].ApproachTime : settings.ApproachTime;

		for (uint i = Attempt.PassedNotes; i < Attempt.Map.Notes.Length; i++)
		{
			Note note = Attempt.Map.Notes[i];

			if (note.Millisecond < Attempt.StartFrom)
			{
				continue;
			}

			if (note.Millisecond + Constants.HIT_WINDOW * Attempt.Speed < Attempt.Progress) // past hit window
			{
				if (i + 1 > Attempt.PassedNotes)
				{
					if (Attempt.IsReplay && Attempt.Replays.Length <= 1 && Attempt.Replays[0].Notes[note.Index] == -1 || !Attempt.IsReplay && note.LastResult != HitResult.Hit)
					{
						note.Miss(this);
					}
					Attempt.PassedNotes = i + 1;
				}

				continue;

				// if (!Attempt.IsReplay)
				// {
				// 	continue;
				// }

			}
			else if (note.Millisecond > Attempt.Progress + at * 1000 * Attempt.Speed)   // past approach distance
			{
				break;
			}
			else if (note.LastResult == HitResult.Hit) // no point
			{
				continue;
			}

			if (settings.AlwaysPlayHitSound && !Attempt.Map.Notes[i].Hittable && note.Millisecond < Attempt.Progress)
			{
				Attempt.Map.Notes[i].Hittable = true;

				SoundManager.HitSound.Play();
			}

			ToProcess++;
			ProcessNotes.Add(note);
		}

		// hitreg check
		for (int i = 0; i < ToProcess; i++)
		{
			Note note = ProcessNotes[i];
			if (note.LastResult == HitResult.Hit) continue;

			if (!Attempt.IsReplay)
			{
				if (note.Millisecond - Attempt.Progress > 0) continue;

				var result = note.CheckHitResult(Attempt);
				if (result == HitResult.Hit)
				{
					note.Hit(this);
				}

			}
			else if (Attempt.Replays.Length > 1 && note.Millisecond - Attempt.Progress <= 0 || Attempt.Replays[0].Notes[note.Index] != -1 && note.Millisecond - Attempt.Progress + Attempt.Replays[0].Notes[note.Index] * Attempt.Speed <= 0)
			{
				note.Hit(this);
			}
		}

		if (Attempt.Progress >= Attempt.MapLength)
		{
			Stop();
			return;
		}

		if (StopQueued)
		{
			StopQueued = false;
			Stop();
			return;
		}
	}

	public void OnHitResultChanged(int noteIndex, HitResult hitResult)
	{
		float lateness = Attempt.IsReplay ? Attempt.HitsInfo[noteIndex] : (float)(((int)Attempt.Progress - Attempt.Map.Notes[noteIndex].Millisecond) / Attempt.Speed);
		float factor = 1 - Math.Max(0, lateness - 25) / 150f;
		uint hitScore = (uint)(100 * Attempt.ComboMultiplier * Attempt.ModsMultiplier * factor * ((Attempt.Speed - 1) / 2.5 + 1));
		
		switch (hitResult)
		{
			case HitResult.Hit:
				SoundManager.HitSound.Play();
				Attempt.Hits++;
				Attempt.Sum++;
				Attempt.Accuracy = Math.Floor((float)Attempt.Hits / Attempt.Sum * 10000) / 100;
				Attempt.Combo++;
				Attempt.ComboMultiplierProgress++;
				Attempt.LastHitColour = SkinManager.Instance.Skin.NoteColors[noteIndex % SkinManager.Instance.Skin.NoteColors.Length];
				Attempt.Score += hitScore;
				Attempt.HealthStep = Math.Max(Attempt.HealthStep / 1.45, 15);
				Attempt.Health = Math.Min(100, Attempt.Health + Attempt.HealthStep / 1.75);
				if (!Attempt.IsReplay)
				{
					Stats.NotesHit++;
					if (Attempt.Combo > Stats.HighestCombo) Stats.HighestCombo = Attempt.Combo;
					Attempt.HitsInfo[noteIndex] = lateness;
				}
				if (Attempt.ComboMultiplierProgress == Attempt.ComboMultiplierIncrement)
				{
					if (Attempt.ComboMultiplier < 8)
					{
						Attempt.ComboMultiplierProgress = Attempt.ComboMultiplier == 7 ? Attempt.ComboMultiplierIncrement : 0;
						Attempt.ComboMultiplier++;
					}
				}
				break;
			case HitResult.Miss:
				SoundManager.MissSound.Play();
				Attempt.Misses++;
				Attempt.Sum++;
				Attempt.Accuracy = Mathf.Floor((float)Attempt.Hits / Attempt.Sum * 10000) / 100;
				Attempt.Combo = 0;
				Attempt.ComboMultiplierProgress = 0;
				Attempt.ComboMultiplier = Math.Max(1, Attempt.ComboMultiplier - 1);
				Attempt.Health = Math.Max(0, Attempt.Health - Attempt.HealthStep);
				Attempt.HealthStep = Math.Min(Attempt.HealthStep * 1.2, 100);
				if (!Attempt.IsReplay)
				{
					Stats.NotesMissed++;
					Attempt.HitsInfo[noteIndex] = -1;
				}
				if (!Attempt.IsReplay && Attempt.Health <= 0 && Attempt.Alive)
				{
					Attempt.Alive = false;
					Attempt.Qualifies = false;
					Attempt.DeathTime = Attempt.Progress;
					SoundManager.FailSound.Play();

					if (!Attempt.Mods["NoFail"]) QueueStop();
				}
				break;
			default:
				break;
		}

		EmitSignal(SignalName.AttemptStatsUpdated, Attempt);
	}

	public void Play()
	{
		if (Attempt == null) return;

		if (!NotesOnly)
		{
			HudManager.Init();
			Attempt.TimeStarted = Time.GetTicksUsec();
			HitResultChanged += OnHitResultChanged;
		}

		settings = SettingsManager.Instance.Settings;
		//SpinCamera = Attempt.Mods.Any(mod => mod.Key == "Spin");
		SpinCamera = Attempt.Mods["Spin"];
		Camera.Fov = (float)(Attempt.IsReplay ? Attempt.Replays[0].FoV : settings.FoV.Value);
		Notes.Multimesh.Mesh = SkinManager.Instance.Skin.NoteMesh;
		Playing = true;

		if (Attempt.Map.AudioBuffer != null)
		{
			SoundManager.Song.Stream = Util.Audio.LoadStream(Attempt.Map.AudioBuffer);
			SoundManager.Song.PitchScale = (float)Attempt.Speed;
			Attempt.MapLength = (float)SoundManager.Song.Stream.GetLength() * 1000;
		}
		else
		{
			Attempt.MapLength = Attempt.Map.Length + 1000;
		}

		Attempt.MapLength += Constants.HIT_WINDOW;
		SoundManager.UpdateVolume();

	}

	public void Pause()
	{

	}

	public void Skip()
	{
		if (Attempt.CanSkip)
		{
			Attempt.ReplaySkips.Add((float)Attempt.Progress);

			if (Attempt.PassedNotes >= Attempt.Map.Notes.Length)
			{
				Attempt.Progress = SoundManager.Song.Stream.GetLength() * 1000;
			}
			else
			{
				Attempt.Progress = Attempt.Map.Notes[Attempt.PassedNotes].Millisecond - settings.ApproachTime * 1500 * Attempt.Speed; // turn AT to ms and multiply by 1.5x

				// Discord.Client.UpdateEndTime(DateTime.UtcNow.AddSeconds((Time.GetUnixTimeFromSystem() + (Attempt.Map.Length - Attempt.Progress) / 1000 / Attempt.Speed)));

				if (Attempt.Map.AudioBuffer != null)
				{
					if (!SoundManager.Song.Playing)
					{
						SoundManager.Song.Play();
					}

					SoundManager.Song.Seek((float)Attempt.Progress / 1000);
					VideoStreamPlayer.StreamPosition = (float)Attempt.Progress / 1000;
				}
			}
		}
	}

	public void QueueStop()
	{
		if (!Playing)
		{
			return;
		}

		Playing = false;
		StopQueued = true;
	}


	public void Stop(bool results = true)
	{
		if (Attempt.Stopped)
		{
			return;
		}

		Attempt.HitsInfo = Attempt.HitsInfo[0 .. (int)Attempt.PassedNotes];

		// dont want an infinite dependency loop so im just going to do this -fog
		if (!Attempt.IsReplay && GameScene.Instance.ReplayManager.CurrentMode == ReplayManager.Mode.RECORD)
		{
			GameScene.Instance.ReplayManager.SaveReplay(Attempt);
		}


		if (!Attempt.IsReplay)
		{
			Stats.GamePlaytime += (Time.GetTicksUsec() - Attempt.TimeStarted) / 1000000;
			Stats.TotalDistance += (ulong)Attempt.DistanceMM;

			if (Attempt.StartFrom == 0)
			{
				if (!File.Exists($"{Constants.USER_FOLDER}/pbs/{Attempt.Map.Name}"))
				{
					List<byte> bytes = [0, 0, 0, 0];
					bytes.AddRange(SHA256.HashData([0, 0, 0, 0]));
					File.WriteAllBytes($"{Constants.USER_FOLDER}/pbs/{Attempt.Map.Name}", [.. bytes]);
				}

				Leaderboard leaderboard = new(Attempt.Map.Name, $"{Constants.USER_FOLDER}/pbs/{Attempt.Map.Name}");

				leaderboard.Add(new(Attempt.ID, "You", Attempt.Qualifies, Attempt.Score, Attempt.Accuracy, Time.GetUnixTimeFromSystem(), Attempt.Progress, Attempt.Map.Length, Attempt.Speed, Attempt.Mods));
				leaderboard.Save();

				if (Attempt.Qualifies)
				{
					Stats.Passes++;
					Stats.TotalScore += Attempt.Score;

					if (Attempt.Accuracy == 100)
					{
						Stats.FullCombos++;
					}

					if (Attempt.Score > Stats.HighestScore)
					{
						Stats.HighestScore = Attempt.Score;
					}

					Stats.PassAccuracies.Add(Attempt.Accuracy);
				}
			}
		}

		DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Adaptive);

		if (results)
		{
			SceneManager.Load("res://scenes/results.tscn");
		}
	}
}