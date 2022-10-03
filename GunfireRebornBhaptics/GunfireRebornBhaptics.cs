using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using MyBhapticsTactsuit;
using BepInEx.IL2CPP;


namespace GunfireRebornBhaptics
{
    [BepInPlugin("org.bepinex.plugins.GunfireRebornBhaptics", "GunfireReborn bhaptics integration", "1.0")]
    public class GunfireRebornBhaptics : BasePlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static TactsuitVR tactsuitVr;


        public override void Load()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            //Log = base.Logger;
            // Plugin startup logic
            //Logger.LogMessage("Plugin GunfireRebornBhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.GunfireRebornBhaptics");
            harmony.PatchAll();
        }
    }

    /**[HarmonyPatch(typeof(Food), "OnEat", new Type[] { })]
    public class bhaptics_OnEat
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (GunfireRebornBhaptics.tactsuitVr.suitDisabled)
            {
                return;
            }
            GunfireRebornBhaptics.tactsuitVr.PlaybackHaptics("Eating");
        }
    }
    **/
}

