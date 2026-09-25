using System;
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

        private static readonly FieldInfo AllStationsField =
            AccessTools.Field(typeof(CraftingStation), "m_allStations");

        private static readonly Collider[] OverlapBuffer = new Collider[512];
        private static readonly HashSet<Piece> ProcessedPieces = new HashSet<Piece>();
        private static readonly Dictionary<CraftingStation, float> StationTimers =
            new Dictionary<CraftingStation, float>(32);

        private static float _nextLoopCheckTime;
        private static float _holdTimer;
        private static bool _holdExecuted;

        /// <summary>Any crafting station can watch — forge, stonecutter, workbench, etc.</summary>
        internal static bool IsHearthmendStation(CraftingStation station)
        {
            return station != null;
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

        /// <summary>Timer mode: Repair Interval &gt; 0.</summary>
        internal static void UpdateTimerLoop()
        {
            if (!PluginConfig.ModEnabled.Value)
            {
                return;
            }

            var interval = PluginConfig.RepairInterval.Value;
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
                if (nview == null || !nview.IsValid() || !nview.IsOwner() || !GetEnabled(nview))
                {
                    continue;
                }

                if (!StationTimers.TryGetValue(station, out var last))
                {
                    last = 0f;
                }

                if (now - last < interval)
                {
                    continue;
                }

                StationTimers[station] = now;
                PerformStationRepair(station, suppressMessage: false);
            }
        }

        /// <summary>Morning mode: Repair Interval == 0, on wake from sleep.</summary>
        internal static void TriggerMorningRepair()
        {
            if (!PluginConfig.ModEnabled.Value || PluginConfig.RepairInterval.Value > 0f)
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
                if (nview == null || !nview.IsValid() || !nview.IsOwner() || !GetEnabled(nview))
                {
                    continue;
                }

                total += PerformStationRepair(station, suppressMessage: true);
            }

            if (total > 0 && PluginConfig.ShowNotification.Value)
            {
                ShowMessage(MessageHud.MessageType.TopLeft, ModLocalization.L("hearthmend_morning", total));
            }
        }

        internal static int PerformStationRepair(CraftingStation station, bool suppressMessage)
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

            var repaired = 0;
            ProcessedPieces.Clear();
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
                if (piece == null || !ProcessedPieces.Add(piece))
                {
                    continue;
                }

                if (!PieceBelongsToStation(station, piece))
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

                if (wear.Repair())
                {
                    repaired++;
                }
            }

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
