using System;
using System.Globalization;
using Godot;

namespace util;

public class I18n
{
    public static string Locale
    {
        get => TranslationServer.GetLocale();
        set => TranslationServer.SetLocale(value);
    }

    public static IFormatProvider LocaleCulture => CultureInfo.GetCultureInfo(Locale.Replace('_', '-'));

    public static string Tr(string str) => TranslationServer.Translate(str);

    public static string Tr(string str, params object[] args) => string.Format(LocaleCulture, Tr(str), args);

    public static string TrF(IFormatProvider provider, string str, params object[] args) => string.Format(provider, Tr(str), args);

    public static string TrN(string singular, string plural, int count) => TranslationServer.TranslatePlural(singular, plural, count);

    public static string TrN(string singular, string plural, int count, params object[] args) =>
        string.Format(LocaleCulture, TrN(singular, plural, count), args);

    public static void SyncLocale()
    {
        Locale = Constants.SUPPORTED_LANGUAGES.Contains(SettingsManager.Instance.LangCode)
            ? SettingsManager.Instance.LangCode
            : Constants.SUPPORTED_LANGUAGES[0];
    }
}
