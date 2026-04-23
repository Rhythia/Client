using System;
using Godot;

namespace Util;

public class String
{
    public static string FormatTime(double seconds, bool padMinutes = false)
    {
        int minutes = (int)Mathf.Floor(seconds / (seconds > 0 ? 60 : -60));

        seconds -= minutes * 60;
        seconds = Math.Floor(seconds);

        return $"{(seconds < 0 ? "-" : "")}{(padMinutes ? minutes.ToString().PadZeros(2) : minutes)}:{Math.Abs(seconds).ToString().PadZeros(2)}";
    }

    public static string FormatUnixTimePretty(double now, double time)
    {
        string formatted;
        double seconds, minutes, hours, days;
        double difference = now - time;
        string prefix = difference < 0 ? "in " : "";
        string suffix = difference > 0 ? " ago" : "";

        seconds = Math.Floor(difference);
        minutes = Math.Floor(seconds / 60);
        hours = Math.Floor(minutes / 60);
        days = Math.Floor(hours / 24);

        if (days > 0)
        {
            formatted = $"{PadMagnitude(days.ToString())} day" + (days > 1 ? "s" : "");
        }
        else if (hours > 0)
        {
            formatted = $"{PadMagnitude(hours.ToString())} hour" + (hours > 1 ? "s" : "");
        }
        else if (minutes > 0)
        {
            formatted = $"{PadMagnitude(minutes.ToString())} minute" + (minutes > 1 ? "s" : "");
        }
        else if (seconds > 0)
        {
            formatted = $"{PadMagnitude(seconds.ToString())} second" + (seconds > 1 ? "s" : "");
        }
        else
        {
            formatted = "just now";
        }

        return $"{prefix}{formatted}{suffix}";
    }

    public static string PadMagnitude(string str, string pad = ",")
    {
        string formatted = "";
        string[] split = str.Split(".");
        string whole = split[0];
        string decimals = split.Length > 1 ? "." + split[1] : "";

        for (int i = 0; i < whole.Length; i++)
        {
            formatted += whole[i];

            if ((whole.Length - i - 1) % 3 == 0)
            {
                formatted += pad;
            }
        }

        return formatted.TrimSuffix(pad) + decimals;
    }
}
