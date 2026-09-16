using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Godot;

namespace Util;

public class String
{
    public static string FormatTime(double seconds, bool padMinutes = false)
    {
        bool negative = seconds < 0;

        int minutes = (int)Mathf.Floor(Math.Abs(seconds) / 60);
        seconds = (int)Math.Floor(Math.Abs(seconds) % 60);

        return $"{(negative ? "-" : "")}{(padMinutes ? minutes.ToString("D2", CultureInfo.CurrentCulture) : minutes)}:{seconds.ToString("00", CultureInfo.CurrentCulture)}";
    }

    public static string FormatUnixTimePretty(double now, double time)
    {
        string formatted;
        double seconds,
            minutes,
            hours,
            days;
        double difference = now - time;
        string prefix = difference < 0 ? "in " : "";
        string suffix = difference > 0 ? " ago" : "";

        seconds = Math.Floor(difference);
        minutes = Math.Floor(seconds / 60);
        hours = Math.Floor(minutes / 60);
        days = Math.Floor(hours / 24);

        if (days > 0)
        {
            formatted = $"{PadMagnitude(days)} day" + (days > 1 ? "s" : "");
        }
        else if (hours > 0)
        {
            formatted = $"{PadMagnitude(hours)} hour" + (hours > 1 ? "s" : "");
        }
        else if (minutes > 0)
        {
            formatted = $"{PadMagnitude(minutes)} minute" + (minutes > 1 ? "s" : "");
        }
        else if (seconds > 0)
        {
            formatted = $"{PadMagnitude(seconds)} second" + (seconds > 1 ? "s" : "");
        }
        else
        {
            formatted = "just now";
        }

        return $"{prefix}{formatted}{suffix}";
    }

    public static string PadMagnitude(double value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    public static string SanitizeZalgo(string input, int limit = 3)
    {
        var zalgoSpamRegex = new Regex("([\\p{M}]){" + (limit + 1) + ",}");

        return zalgoSpamRegex.Replace(input, "$1");
    }

    public static string SanitizeBBCode(string input)
    {
        return input.Replace("[", "[lb]");
    }
}
