using BepInEx.Configuration;

namespace Hearthmend
{
    internal static class PluginConfig
    {
        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<float> RepairRadius;
        internal static ConfigEntry<float> MendEvery;
        internal static ConfigEntry<bool> MendOnWake;
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
                "How far from a watching station (workbench, forge, stonecutter and so on) pieces get mended.");

            MendEvery = cfg.Bind(
                "2. Repair",
                "Mend Every (s)",
                30f,
                "While you are near a watching station, it mends the damage around it this often. 0 turns daytime mending off.");

            MendOnWake = cfg.Bind(
                "2. Repair",
                "Mend On Wake",
                true,
                "Every station watching near you also mends when you wake up from sleep.");

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
                true,
                "Show a small HUD note when something gets mended.");
        }
    }
}
