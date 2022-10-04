using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MyBhapticsTactsuit;
using BepInEx.IL2CPP;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Events;

namespace GunfireRebornBhaptics
{
    [BepInPlugin("org.bepinex.plugins.GunfireRebornBhaptics", "GunfireReborn bhaptics integration", "1.0")]
    public class Plugin : BasePlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static TactsuitVR tactsuitVr;


        public override void Load()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Log;
            // Plugin startup logic
            //Logger.LogMessage("Plugin GunfireRebornBhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            //delay patching
            SceneManager.sceneLoaded += (UnityAction<Scene, LoadSceneMode>)new Action<Scene, LoadSceneMode>(OnSceneLoaded);
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.GunfireRebornBhaptics");
            harmony.PatchAll();
        }
    }

    /**
     * Can't find hit transform object, using static class
     * as an ydrator or factory of some sort in the original code, ugly
     */
    [HarmonyPatch(typeof(HeroBeHitCtrl), "HeroInjured")]
    public class bhaptics_OnInjured
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            //PlayerProp player = NewObjectCache.GetPlayerProp(HeroBeHitCtrl.HeroID);
            //Plugin.Log.LogMessage(" player " + player.ShootStatus);
            //Plugin.Log.LogMessage(" Transform " + HeroBeHitCtrl.DirHitTran.position);
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Impact");
        }
    }

    #region guns

    /**
     * Many different classes for guns, not a single parent one.
     * TODO : make an associative array with weapon id => tact pattern
     */

    [HarmonyPatch(typeof(ASBaseShoot), "OnReload")]
    public class bhaptics_OnReload
    {
        [HarmonyPostfix]
        public static void Postfix(ASBaseShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
        }
    }

    /**
     * pistols and derivatives
     */
    [HarmonyPatch(typeof(ASAutoShoot), "AttackOnce")]
    public class bhaptics_OnFire
    {
        [HarmonyPostfix]
        public static void Postfix(ASAutoShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
        }
    }
    #endregion
}

