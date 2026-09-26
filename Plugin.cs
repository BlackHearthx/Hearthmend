using BepInEx;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;
using UnityEngine;

namespace Hearthmend
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class HearthmendPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.blackhearthx.hearthmend";
        public const string PluginName = "Hearthmend";
        public const string PluginVersion = "1.0.1";

        /// <summary>Vanilla Player.m_removeRayMask minus terrain; "vehicle" is where ships and carts live.</summary>
        internal static int PieceMask { get; private set; }

        internal static HearthmendPlugin Instance { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            PieceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "vehicle");
            PluginConfig.Bind(Config);
            ModLocalization.Register();

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void Update()
        {
            RepairService.UpdateTimerLoop();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
