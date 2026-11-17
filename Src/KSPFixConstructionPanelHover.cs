using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace com.github.lhervier.ksp.fix.constructionpanelhover
{
    /// <summary>
    /// Installs the fix the settings turn on, once, when KSP starts: from then on, a part held in EVA construction
    /// mode no longer stays red because of the contacts it had when it was hidden.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class KSPFixConstructionPanelHover : MonoBehaviour
    {
        private const string HarmonyId = "com.github.lhervier.ksp.fix.constructionpanelhover";

        private void Start()
        {
            Settings.Load();
            string version = typeof(KSPFixConstructionPanelHover).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Log.Info($"Version {version}, " +
                $"hiddenPartContactsReset {Settings.HiddenPartContactsReset}, log level {Log.Level}");
            if (!Settings.HiddenPartContactsReset)
            {
                return;
            }
            try
            {
                HiddenPartContactsReset.Install(new Harmony(HarmonyId));
                Log.Info("Hidden part contacts reset fix installed");
            }
            catch (Exception e)
            {
                Log.Error("Could not install the hidden part contacts reset fix, a held part can still stay red for " +
                    $"good, as on stock: {e}");
            }
        }
    }
}
