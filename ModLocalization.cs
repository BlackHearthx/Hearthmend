using System;
using System.Collections.Generic;
using System.IO;
using Jotunn.Managers;
using UnityEngine;

namespace Hearthmend
{
    internal static class ModLocalization
    {
        internal static void Register()
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            var loaded = 0;
            foreach (var lang in new[] { "English", "Portuguese_Brazilian", "Portuguese_European" })
            {
                if (TryLoad(loc, lang))
                {
                    loaded++;
                }
            }

            if (loaded == 0)
            {
                loc.AddTranslation("English", FallbackEnglish());
                Jotunn.Logger.LogWarning("Hearthmend: Translations folder missing, using the English fallback");
            }
        }

        private static Dictionary<string, string> FallbackEnglish()
        {
            return new Dictionary<string, string>
            {
                { "hearthmend_hover", "\n[<color=yellow><b>Hold $KEY_Use</b></color>] Hearthmend: {0}" },
                { "hearthmend_on", "<color=#55FF55>on</color>" },
                { "hearthmend_off", "<color=#FF5555>off</color>" },
                { "hearthmend_toggle_on", "Hearthmend is watching this station" },
                { "hearthmend_toggle_off", "Hearthmend won't touch this station" },
                { "hearthmend_repaired", "Mended {0} pieces" },
                { "hearthmend_morning", "Woke up to {0} pieces mended" }
            };
        }

        private static bool TryLoad(Jotunn.Entities.CustomLocalization loc, string language)
        {
            try
            {
                var path = FindPath(language);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return false;
                }

                loc.AddJsonFile(language, File.ReadAllText(path));
                return true;
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Hearthmend: failed loading {language}: {ex.Message}");
                return false;
            }
        }

        private static string FindPath(string language)
        {
            var file = Path.Combine("Translations", language, "hearthmend.json");
            var nextToDll = Path.Combine(Path.GetDirectoryName(typeof(HearthmendPlugin).Assembly.Location) ?? "", file);
            if (File.Exists(nextToDll))
            {
                return nextToDll;
            }

            var cwd = Path.Combine(Directory.GetCurrentDirectory(), file);
            return File.Exists(cwd) ? cwd : null;
        }

        internal static string L(string token, params object[] args)
        {
            var raw = Localization.instance != null
                ? Localization.instance.Localize("$" + token)
                : "$" + token;
            if (args == null || args.Length == 0)
            {
                return raw;
            }

            try
            {
                return string.Format(raw, args);
            }
            catch
            {
                return raw;
            }
        }
    }
}
