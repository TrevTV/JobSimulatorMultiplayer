using System.Collections.Generic;
using Harmony;
using MelonLoader;
using OwlchemyVR;
using PSC;
using UnityEngine;

namespace JobSimulatorMultiplayer.Features
{
    public class PatchTools
    {
        public static List<int> blacklist = new List<int>();
    }

    [HarmonyPatch(typeof(Room), "Awake")]
    static class ForceLoadLargeSpace
    {
        static bool Prefix()
        {
            if (!ModPrefs.GetBool("MPMod", "ForceLargePlayspace"))
                return true;

            var layout = new LayoutConfiguration();
            layout.sizeInMeters = new Vector2(10f, 10f);
            GameObject.FindObjectOfType<Room>().LoadLayout(layout);
            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionHandController), "GrabGrabbable")]
    static class GrabHooks
    {
        static void Postfix(GrabbableItem grabbableItem)
        {
            var rb = grabbableItem.gameObject.GetComponent<Rigidbody>();
            if (JobSimulatorMultiplayer.isClient && rb)
            {
                foreach (var newRb in grabbableItem.gameObject.GetComponentsInChildren<Rigidbody>())
                    newRb.isKinematic = false;

                rb.isKinematic = false;
            }
        }
    }

    [HarmonyPatch(typeof(InteractionHandController), "ReleaseCurrGrabbable")]
    static class ReleaseHooks
    {
        static void Prefix(InteractionHandController __instance)
        {
            var rb = __instance.currGrabbedItem.gameObject.GetComponent<Rigidbody>();
            if (JobSimulatorMultiplayer.isClient && rb)
            {
                if (PatchTools.blacklist.Contains(rb.GetInstanceID()))
                    return;

                foreach (var newRb in rb.gameObject.GetComponentsInChildren<Rigidbody>())
                    newRb.isKinematic = true;

                rb.isKinematic = true;
            }
        }
    }
}