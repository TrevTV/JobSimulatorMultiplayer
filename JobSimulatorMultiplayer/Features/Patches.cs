using System;
using Harmony;
using MelonLoader;
using OwlchemyVR;
using PSC;
using UnityEngine;

namespace JobSimulatorMultiplayer.Features
{
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
}