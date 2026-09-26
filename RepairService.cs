using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Hearthmend
{
    /// <summary>
    /// Station auto-repair: each watching CraftingStation mends Piece+WearNTear
    /// in radius that belong to that station (same m_name rule as the hammer).
    /// </summary>
    internal static class RepairService
    {
        internal const string ZdoKey = "BHX_Hearthmend";

        private const float HoldRequiredSeconds = 0.6f;
        private const float TimerScanPeriod = 0.25f;
        private const float MendGapSeconds = 0.15f;
        private const float MaxWaveSeconds = 6f;
        private const float TimerJitterSeconds = 3f;

        private static readonly List<WearNTear> PendingMends = new List<WearNTear>(256);

        private static readonly FieldInfo AllStationsField =
            AccessTools.Field(typeof(CraftingStation), "m_allStations");

        private static readonly Collider[] OverlapBuffer = new Collider[4096];
        private static bool _warnedBufferFull;
        private static readonly HashSet<Piece> ProcessedPieces = new HashSet<Piece>();
        private static readonly Dictionary<CraftingStation, float> StationTimers =
            new Dictionary<CraftingStation, float>(32);

        private static readonly HashSet<string> BuildStationNames = new HashSet<string>();

        private static float _nextLoopCheckTime;
        private static float _holdTimer;
        private static bool _holdExecuted;

        /// <summary>
        /// Collect station names that at least one build Piece requires.
        /// That is the same list the hammer uses (workbench, forge, stonecutter, artisan, black forge, galdr).
        /// Cauldrons and food tables never show up here.
        /// </summary>
        internal static void RefreshBuildStationCatalog()
        {
            var scene = ZNetScene.instance;
            if (scene == null || scene.m_prefabs == null)
            {
                return;
            }

            BuildStationNames.Clear();
            for (var i = 0; i < scene.m_prefabs.Count; i++)
            {
                var prefab = scene.m_prefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                var piece = prefab.GetComponent<Piece>();
                if (piece == null || piece.m_craftingStation == null)
                {
                    continue;
                }

                var name = piece.m_craftingStation.m_name;
                if (!string.IsNullOrEmpty(name))
                {
                    BuildStationNames.Add(name);
                }
            }

            Jotunn.Logger.LogInfo($"Hearthmend: {BuildStationNames.Count} build station type(s) can watch");
        }

        /// <summary>Only stations the hammer needs for placing or mending buildings.</summary>
        internal static bool IsHearthmendStation(CraftingStation station)
        {
            if (station == null)
            {
                return false;
            }

            if (BuildStationNames.Count == 0)
            {
                RefreshBuildStationCatalog();
            }

            return BuildStationNames.Contains(station.m_name);
        }

        /// <summary>
        /// Vanilla hammer gate: piece needs this station's m_name, or no station at all.
        /// </summary>
        internal static bool PieceBelongsToStation(CraftingStation station, Piece piece)
        {
            if (station == null || piece == null)
            {
                return false;
            }

            if (piece.m_craftingStation == null)
            {
                return true;
            }

            return piece.m_craftingStation.m_name == station.m_name;
        }

        internal static bool GetEnabled(ZNetView nview)
        {
            return nview != null && nview.IsValid() && nview.GetZDO().GetBool(ZdoKey, false);
        }

        internal static void SetEnabled(ZNetView nview, bool enabled)
        {
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            nview.ClaimOwnership();
            nview.GetZDO().Set(ZdoKey, enabled);
        }

        private static List<CraftingStation> GetStationList()
        {
            return AllStationsField?.GetValue(null) as List<CraftingStation>;
        }

        internal static void ShowMessage(MessageHud.MessageType type, string message)
        {
            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.Message(type, message);
                return;
            }

            MessageHud.instance?.ShowMessage(type, message);
        }

        /// <summary>
        /// Daytime mending. A watching station mends as soon as it is first seen near you,
        /// then every Mend Every seconds, with a little jitter so stations do not all fire on one frame.
        /// </summary>
        internal static void UpdateTimerLoop()
        {
            if (!PluginConfig.ModEnabled.Value)
            {
                return;
            }

            var interval = PluginConfig.MendEvery.Value;
            if (interval <= 0f || Time.time < _nextLoopCheckTime)
            {
                return;
            }

            _nextLoopCheckTime = Time.time + TimerScanPeriod;

            var local = Player.m_localPlayer;
            if (local == null)
            {
                return;
            }

            var stations = GetStationList();
            if (stations == null || stations.Count == 0)
            {
                return;
            }

            var playerPos = local.transform.position;
            var nearbyLimit = PluginConfig.RepairRadius.Value + 35f;
            var nearbySqr = nearbyLimit * nearbyLimit;
            var now = Time.time;

            for (var i = 0; i < stations.Count; i++)
            {
                var station = stations[i];
                if (!IsHearthmendStation(station))
                {
                    continue;
                }

                var delta = station.transform.position - playerPos;
                if (delta.sqrMagnitude > nearbySqr)
                {
                    continue;
                }

                var nview = station.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid() || !GetEnabled(nview))
                {
                    continue;
                }

                if (StationTimers.TryGetValue(station, out var due) && now < due)
                {
                    continue;
                }

                ScheduleNext(station, now);
                PerformStationRepair(station, suppressMessage: false);
            }
        }

        private static void ScheduleNext(CraftingStation station, float now)
        {
            StationTimers[station] = now + PluginConfig.MendEvery.Value + UnityEngine.Random.Range(0f, TimerJitterSeconds);
        }

        /// <summary>Wake from sleep: every watching station near you mends once, sharing one piece list.</summary>
        internal static void TriggerMorningRepair()
        {
            if (!PluginConfig.ModEnabled.Value || !PluginConfig.MendOnWake.Value)
            {
                return;
            }

            var local = Player.m_localPlayer;
            if (local == null)
            {
                return;
            }

            var stations = GetStationList();
            if (stations == null || stations.Count == 0)
            {
                return;
            }

            var playerPos = local.transform.position;
            var nearbyLimit = PluginConfig.RepairRadius.Value + 50f;
            var nearbySqr = nearbyLimit * nearbyLimit;
            var total = 0;
            var watching = 0;
            var now = Time.time;
            ProcessedPieces.Clear();

            for (var i = 0; i < stations.Count; i++)
            {
                var station = stations[i];
                if (!IsHearthmendStation(station))
                {
                    continue;
                }

                var delta = station.transform.position - playerPos;
                if (delta.sqrMagnitude > nearbySqr)
                {
                    continue;
                }

                var nview = station.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid() || !GetEnabled(nview))
                {
                    continue;
                }

                watching++;
                ScheduleNext(station, now);
                total += PerformStationRepair(station, suppressMessage: true, shareProcessed: true);
            }

            Jotunn.Logger.LogInfo($"Hearthmend: woke up, {watching} station(s) watching nearby, {total} piece(s) to mend");

            if (total > 0 && PluginConfig.ShowNotification.Value)
            {
                ShowMessage(MessageHud.MessageType.TopLeft, ModLocalization.L("hearthmend_morning", total));
            }
        }

        internal static int PerformStationRepair(CraftingStation station, bool suppressMessage, bool shareProcessed = false)
        {
            if (station == null)
            {
                return 0;
            }

            var radius = PluginConfig.RepairRadius.Value;
            var hitCount = Physics.OverlapSphereNonAlloc(
                station.transform.position,
                radius,
                OverlapBuffer,
                HearthmendPlugin.PieceMask);

            if (hitCount == 0)
            {
                return 0;
            }

            if (hitCount == OverlapBuffer.Length && !_warnedBufferFull)
            {
                _warnedBufferFull = true;
                Jotunn.Logger.LogWarning(
                    $"Hearthmend: {OverlapBuffer.Length} colliders around {station.m_name}, some pieces may be skipped. Try a smaller Repair Radius.");
            }

            PendingMends.Clear();
            if (!shareProcessed)
            {
                ProcessedPieces.Clear();
            }

            var allowOther = PluginConfig.AllowRepairOther.Value;
            var respectWards = PluginConfig.RespectWards.Value;

            for (var i = 0; i < hitCount; i++)
            {
                var col = OverlapBuffer[i];
                if (col == null)
                {
                    continue;
                }

                var piece = col.GetComponentInParent<Piece>();
                if (piece == null || !PieceBelongsToStation(station, piece) || !ProcessedPieces.Add(piece))
                {
                    continue;
                }

                if (!allowOther && !piece.IsCreator())
                {
                    continue;
                }

                if (respectWards && !PrivateArea.CheckAccess(piece.transform.position, 0f, false, true))
                {
                    continue;
                }

                var wear = piece.GetComponent<WearNTear>();
                if (wear == null || wear.GetHealthPercentage() >= 1f)
                {
                    continue;
                }

                PendingMends.Add(wear);
            }

            var repaired = PendingMends.Count;
            if (repaired == 0)
            {
                return 0;
            }

            HearthmendPlugin.Instance.StartCoroutine(MendInTurn(PendingMends.ToArray()));
            PendingMends.Clear();
            Jotunn.Logger.LogInfo($"Hearthmend: {station.m_name} is mending {repaired} piece(s)");

            if (!suppressMessage
                && repaired > 0
                && PluginConfig.ShowNotification.Value
                && Player.m_localPlayer != null)
            {
                var dist = Vector3.Distance(
                    station.transform.position,
                    Player.m_localPlayer.transform.position);
                if (dist <= radius)
                {
                    ShowMessage(
                        MessageHud.MessageType.TopLeft,
                        ModLocalization.L("hearthmend_repaired", repaired));
                }
            }

            return repaired;
        }

        /// <summary>
        /// One piece at a time so the mend reads as a wave, capped so a big base finishes in a few seconds.
        /// </summary>
        private static IEnumerator MendInTurn(WearNTear[] batch)
        {
            var wait = new WaitForSeconds(Mathf.Min(MendGapSeconds, MaxWaveSeconds / batch.Length));
            for (var i = 0; i < batch.Length; i++)
            {
                var wear = batch[i];
                if (wear != null && wear.Repair())
                {
                    PlayMendEffect(wear);
                }

                if (i < batch.Length - 1)
                {
                    yield return wait;
                }
            }
        }

        /// <summary>Same puff and sound the hammer plays on a successful repair (Player.Repair).</summary>
        private static void PlayMendEffect(WearNTear wear)
        {
            var piece = wear.GetComponent<Piece>();
            if (piece == null || piece.m_placeEffect == null || !piece.m_placeEffect.HasEffects())
            {
                return;
            }

            var t = piece.transform;
            piece.m_placeEffect.Create(t.position, t.rotation);
        }

        internal static void AppendHoverHint(CraftingStation station, ref string result)
        {
            if (!PluginConfig.ModEnabled.Value || !IsHearthmendStation(station))
            {
                return;
            }

            var nview = station.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            var state = GetEnabled(nview)
                ? ModLocalization.L("hearthmend_on")
                : ModLocalization.L("hearthmend_off");
            var line = ModLocalization.L("hearthmend_hover", state);
            result += Localization.instance != null
                ? Localization.instance.Localize(line)
                : line;
        }

        internal static void TickHoldToggle(Player player)
        {
            if (player != Player.m_localPlayer || !PluginConfig.ModEnabled.Value)
            {
                return;
            }

            if (!ZInput.GetButton("Use"))
            {
                _holdTimer = 0f;
                _holdExecuted = false;
                return;
            }

            var hover = player.GetHoverObject();
            if (hover == null)
            {
                return;
            }

            var station = hover.GetComponentInParent<CraftingStation>();
            if (!IsHearthmendStation(station))
            {
                return;
            }

            var nview = station.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            _holdTimer += Time.deltaTime;
            if (_holdTimer < HoldRequiredSeconds || _holdExecuted)
            {
                return;
            }

            _holdExecuted = true;
            var next = !GetEnabled(nview);
            SetEnabled(nview, next);
            ShowMessage(
                MessageHud.MessageType.Center,
                next
                    ? ModLocalization.L("hearthmend_toggle_on")
                    : ModLocalization.L("hearthmend_toggle_off"));

            if (next)
            {
                ScheduleNext(station, Time.time);
                PerformStationRepair(station, suppressMessage: false);
            }
        }
    }

    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetHoverText))]
    internal static class CraftingStation_GetHoverText_Patch
    {
        private static void Postfix(CraftingStation __instance, ref string __result)
        {
            RepairService.AppendHoverHint(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class Player_Update_HoldToggle_Patch
    {
        private static void Postfix(Player __instance)
        {
            RepairService.TickHoldToggle(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetSleeping))]
    internal static class Player_SetSleeping_Patch
    {
        private static void Postfix(Player __instance, bool sleep)
        {
            if (__instance == Player.m_localPlayer && !sleep)
            {
                RepairService.TriggerMorningRepair();
            }
        }
    }
}
