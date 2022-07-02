using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HarderEasyDeath
{
    public class HarderEasyDeath : Mod
    {
        private Harmony _harmony;

        // ReSharper disable All
        public static SettingsAPI ExtraSettingsAPI_Settings;
        // ReSharper restore All

        public static int SlotsToRemove => SettingsAPI.ExtraSettingsAPI_Loaded
            ? (int)SettingsAPI.ExtraSettingsAPI_GetSliderValue("removeCount")
            : 1;

        public static bool ClearSlots => !SettingsAPI.ExtraSettingsAPI_Loaded ||
                                         SettingsAPI.ExtraSettingsAPI_GetCheckboxState("clearSlots");

        public void Start()
        {
            _harmony = new Harmony("se.gorymoon.rafy.HarderEasyDeath");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log("Mod has been loaded!");
        }

        public void OnModUnload()
        {
            _harmony.UnpatchAll(_harmony.Id);
            Destroy(gameObject);
            Log("Mod has been unloaded!");
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.StartRespawn))]
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once InconsistentNaming
    public class Patch_Player_StartRespawn
    {
        // ReSharper disable once UnusedType.Global
        // ReSharper disable once InconsistentNaming
        public static void Prefix(ref Network_Player ___playerNetwork)
        {
            var gameModeValue = GameModeValueManager.GetCurrentGameModeValue();

            // Only apply to local player playing on Easy
            if (!___playerNetwork.IsLocalPlayer || gameModeValue.gameMode != GameMode.Easy) return;

            var removed = 0;
            foreach (var slot in AllSlots(___playerNetwork.Inventory).Where(slot => slot != null && !slot.IsEmpty))
            {
                if (HarderEasyDeath.ClearSlots)
                {
                    slot.Reset();
                    removed++;
                }
                else
                {
                    var itemInstance = slot.itemInstance;
                    if (itemInstance.baseItem.settings_Inventory.Stackable && slot.slotType != SlotType.Equipment)
                    {
                        var amount = Mathf.FloorToInt(itemInstance.Amount / 3f);
                        if (amount <= 0)
                            slot.Reset();
                        else
                            slot.SetItem(itemInstance.baseItem, amount);
                        removed++;
                    }
                    else if (itemInstance.BaseItemMaxUses > 1)
                    {
                        var num = Mathf.FloorToInt(itemInstance.BaseItemMaxUses * 0.5f);
                        var numberOfUses = itemInstance.Uses - num;
                        if (numberOfUses <= 0)
                            slot.Reset();
                        else
                            slot.SetUses(numberOfUses);
                        removed++;
                    }
                }

                if (removed >= HarderEasyDeath.SlotsToRemove)
                    return;
            }
        }

        [HarmonyPatch]
        private static List<Slot> AllSlots(PlayerInventory inventory)
        {
            var slots = new List<Slot>();
            slots.AddRange(inventory.allSlots);
            slots.AddRange(inventory.equipSlots);
            slots.Shuffle();
            return slots;
        }
    }

    public static class ListExtensions
    {
        private static readonly System.Random Rng = new System.Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}