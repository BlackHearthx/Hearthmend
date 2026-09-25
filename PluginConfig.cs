using BepInEx.Configuration;

namespace Hearthmend
{
    internal static class PluginConfig
    {
        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<float> RepairRadius;
        internal static ConfigEntry<float> RepairInterval;
        internal static ConfigEntry<bool> AllowRepairOther;
        internal static ConfigEntry<bool> RespectWards;
        internal static ConfigEntry<bool> ShowNotification;

        internal static void Bind(ConfigFile cfg)
        {
            ModEnabled = cfg.Bind(
                "1. General",
                "Mod Enabled",
                true,
                "Turn Hearthmend on or off for this profile.");

            RepairRadius = cfg.Bind(
                "2. Repair",
                "Repair Radius",
                20f,
                "How far from a watching station (workbench, forge, stonecutter, …) pieces get mended.");

            RepairInterval = cfg.Bind(
                "2. Repair",
                "Repair Interval (s)",
                0f,
                "Seconds between repairs while a station is watching. 0 = mend once when you wake from sleep.");

            AllowRepairOther = cfg.Bind(
                "2. Repair",
                "Allow Repair Other",
                true,
                "Also mend pieces built by other players.");

            RespectWards = cfg.Bind(
                "2. Repair",
                "Respect Wards",
                true,
                "Skip pieces inside wards you are not allowed to build in.");

            ShowNotification = cfg.Bind(
                "3. UI",
                "Show Notification",
                false,
                "Show a small HUD note when something gets mended.");
        }
    }
}
