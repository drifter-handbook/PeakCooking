using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using Zorro.Core;

namespace PeakCooking;

public class CookingPotPatches
{
    [HarmonyPatch(typeof(Interaction))]
    public class InteractionPatcher
    {
        public class InteractionExtraFields
        {
            public Item? bestCookingPot;
            public bool hasValidTarget => bestCookingPot != null;
        }
        public static Dictionary<Interaction, InteractionExtraFields> Fields = new Dictionary<Interaction, InteractionExtraFields>();

        public static InteractionExtraFields GetExtraFields(Interaction interaction)
        {
            if (!Fields.ContainsKey(interaction))
            {
                Fields[interaction] = new InteractionExtraFields();
            }
            foreach (var k in new List<Interaction>(Fields.Keys))
            {
                if (k == null && !ReferenceEquals(k, null))
                {
                    Fields.Remove(k);
                }
            }
            var fields = Fields[interaction];
            if (fields == null)
            {
                throw new PeakCookingException("Extra Field patch logic gave null, this should never happen");
            }
            return fields;
        }

        [HarmonyPostfix]
        [HarmonyPatch("LateUpdate")]
        static void LateUpdatePatch(ref Interaction __instance)
        {
            Item? item = __instance.bestInteractable as Item;
            if (item != null && ItemPatcher.GetExtraFields(item).IsCookingPot)
            {
                GetExtraFields(__instance).bestCookingPot = __instance.bestInteractable as Item;
            }
        }
    }

    [HarmonyPatch(typeof(Item))]
    public class ItemPatcher
    {
        public class ItemExtraFields
        {
            public CookingPot? script;
            public bool IsCookingPot => script != null;
        }
        public static Dictionary<Item, ItemExtraFields> Fields = new Dictionary<Item, ItemExtraFields>();

        public static ItemExtraFields GetExtraFields(Item item)
        {
            if (!Fields.ContainsKey(item))
            {
                Fields[item] = new ItemExtraFields();
            }
            foreach (var k in new List<Item>(Fields.Keys))
            {
                if (k == null && !ReferenceEquals(k, null))
                {
                    Fields.Remove(k);
                }
            }
            var fields = Fields[item];
            if (fields == null)
            {
                throw new PeakCookingException("Extra Field patch logic gave null, this should never happen");
            }
            return fields;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        static void StartPatch(ref Item __instance)
        {
            GetExtraFields(__instance).script = __instance.GetComponent<CookingPot>();
        }

        public static void FeedItem(ref Item? __instance, Item item)
        {
            if (__instance != null)
            {
                var script = GetExtraFields(__instance).script;
                if (script != null)
                {
                    script.AddToPot(item);
                }
                else
                {
                    throw new PeakCookingException("CookingPot behavior not found, this should never happen");
                }
                // delete item
                if (item != null)
                {
                    item.StartCoroutine(item.ConsumeDelayed(ignoreActions: true));
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("IsInteractible")]
        static void IsInteractiblePatch(ref Item __instance, ref bool __result, Character interactor)
        {
            __result = __result || IsSecondaryInteractible(ref __instance, interactor);
        }

        public static bool IsSecondaryInteractible(ref Item __instance, Character interactor)
        {
            if (HasItemCanUseOnFriend())
            {
                return true;
            }
            return false;
        }

        public static string GetSecondaryInteractionText(ref Item __instance)
        {
            if (HasItemCanUseOnFriend())
            {
                return GetItemPrompt(ref __instance, Character.localCharacter.data.currentItem);
            }
            return "";
        }

        public static string GetItemPrompt(ref Item __instance, Item item)
        {
            return item.UIData.secondaryInteractPrompt.Replace("#targetChar", __instance.GetName());
        }

        private static bool HasItemCanUseOnFriend()
        {
            if ((bool)Character.localCharacter.data.currentItem)
            {
                return Character.localCharacter.data.currentItem.canUseOnFriend;
            }
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("CanUseSecondary")]
        static void CanUseSecondaryPatch(ref Item __instance, ref bool __result)
        {
            __result = __result || (__instance.canUseOnFriend && InteractionPatcher.GetExtraFields(Interaction.instance).hasValidTarget);
        }

        [HarmonyPostfix]
        [HarmonyPatch("StartUseSecondary")]
        static void StartUseSecondaryPatch(ref Item __instance)
        {
            if (!__instance.isUsingPrimary && !__instance.isUsingSecondary)
            {
                if ((bool)__instance.holderCharacter && __instance.canUseOnFriend && InteractionPatcher.GetExtraFields(Interaction.instance).hasValidTarget)
                {
                    // start interaction
                    GameUtils.instance.StartFeed(__instance.holderCharacter.photonView.ViewID, __instance.holderCharacter.photonView.ViewID, __instance.itemID, __instance.totalSecondaryUsingTime);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("FinishCastSecondary")]
        static void FinishCastSecondaryPatch(ref Item __instance)
        {
            if (__instance.canUseOnFriend && InteractionPatcher.GetExtraFields(Interaction.instance).hasValidTarget)
            {
                FeedItem(ref InteractionPatcher.GetExtraFields(Interaction.instance).bestCookingPot, __instance);
                __instance.photonView.RPC("RemoveFeedDataRPC", RpcTarget.All, __instance.holderCharacter.photonView.ViewID);
            }
        }
    }

    [HarmonyPatch(typeof(GUIManager))]
    public class GUIManagerPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch("RefreshInteractablePrompt")]
        static void RefreshInteractablePromptPatch(ref GUIManager __instance)
        {
            if (__instance.currentInteractable.UnityObjectExists())
            {
                var item = __instance.currentInteractable as Item;
                if (item != null && ItemPatcher.GetExtraFields(item).IsCookingPot)
                {
                    __instance.interactName.SetActive(value: false);
                    if (ItemPatcher.IsSecondaryInteractible(ref item, Character.localCharacter))
                    {
                        __instance.interactPromptSecondary.SetActive(value: true);
                        __instance.secondaryInteractPromptText.text = ItemPatcher.GetSecondaryInteractionText(ref item);
                    }
                }
            }
        }
    }
}