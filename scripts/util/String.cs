using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Godot;

namespace util;

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
        double seconds,
            minutes,
            hours,
            days;
        double difference = now - time;
        bool future = difference < 0;

        seconds = Math.Floor(difference);
        minutes = Math.Floor(seconds / 60);
        hours = Math.Floor(minutes / 60);
        days = Math.Floor(hours / 24);

        if (days > 0)
        {
            return formatTimeString(days, future, "{0} day ago", "{0} days ago", "in {0} day", "in {0} days");
        }
        else if (hours > 0)
        {
            return formatTimeString(hours, future, "{0} hour ago", "{0} hours ago", "in {0} hour", "in {0} hours");
        }
        if (minutes > 0)
        {
            return formatTimeString(minutes, future, "{0} minute ago", "{0} minutes ago", "in {0} minute", "in {0} minutes");
        }
        if (seconds > 0)
        {
            return formatTimeString(seconds, future, "{0} second ago", "{0} seconds ago", "in {0} second", "in {0} seconds");
        }

        return I18n.Tr("just now");
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

    private static string formatTimeString(
        double units,
        bool future,
        string pastSingular,
        string pastPlural,
        string futureSingular,
        string futurePlural
    )
    {
        string formatted = future ? I18n.TrN(futureSingular, futurePlural, (int)units) : I18n.TrN(pastSingular, pastPlural, (int)units);

        return string.Format(I18n.LocaleCulture, formatted, PadMagnitude(units));
    }
}
