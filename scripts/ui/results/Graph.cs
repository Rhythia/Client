using Godot;

public partial class Graph : ColorRect
{
    public override void _Draw()
    {
        Color hitColor = Color.FromHtml("00ff00ff");
        Color missColor = Color.FromHtml("ff000044");

        for (ulong i = LegacyRunner.CurrentAttempt.FirstNote; i < (ulong)LegacyRunner.CurrentAttempt.HitsInfo.Length; i++)
        {
            float offset = LegacyRunner.CurrentAttempt.HitsInfo[i];

            if (offset < 0)
            {
                int position = (int)(Size.X * LegacyRunner.CurrentAttempt.Map.Notes[i].Millisecond / LegacyRunner.CurrentAttempt.Map.Length);
                DrawLine(Vector2.Right * position, new(position, Size.Y), missColor, 1);
            }
            else
            {
                DrawRect(new(Size.X * (LegacyRunner.CurrentAttempt.Map.Notes[i].Millisecond / (float)LegacyRunner.CurrentAttempt.Map.Length), Size.Y * (offset / 55), Vector2.One), hitColor);
            }
        }

        if (LegacyRunner.CurrentAttempt.DeathTime >= 0)
        {
            int position = (int)(Size.X * LegacyRunner.CurrentAttempt.DeathTime / LegacyRunner.CurrentAttempt.Map.Length);
            DrawLine(Vector2.Right * position, new(position, Size.Y), Color.Color8(255, 255, 0), 3);
        }
    }
}
