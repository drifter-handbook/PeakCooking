using HarmonyLib;
using System;

namespace PeakCooking;

public class ItemDataPatches
{
    public const byte STRING_TYPE_INDEX = 51;

    [HarmonyPatch(typeof(DataEntryValue))]
    public class DataEntryValuePatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetTypeValue")]
        static void GetTypeValuePatch(ref byte __result, Type type)
        {
            if (__result == 0 && type == typeof(StringItemData))
            {
                __result = STRING_TYPE_INDEX;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetNewFromValue")]
        static bool GetNewFromValuePatch(ref DataEntryValue __result, byte value)
        {
            if (value == STRING_TYPE_INDEX)
            {
                __result = new StringItemData();
                // this is typically very bad practice to cut the method off early, but this usage should be OK
                return false;
            }
            return true;
        }
    }
}