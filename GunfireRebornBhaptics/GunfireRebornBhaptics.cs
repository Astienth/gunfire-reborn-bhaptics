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
        public static bool chargeWeaponCanShoot = false; 
        public static bool continueWeaponCanShoot = false;

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

        public static string getHandSide(int weaponId)
        {
            //dog case
            if (HeroAttackCtrl.HeroObj.playerProp.SID == 201)
            {
                NewPlayerObject heroObj = HeroAttackCtrl.HeroObj;
                return (weaponId == heroObj.PlayerCom.CurWeaponID) ? "R" : "L";
            }
            return "R";
        }
    }

    #region guns

    /**
     * Many different classes for guns, not a single parent one.
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
            if (__instance.ReloadComponent.m_IsReload)
            {
                Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
            }
        }
    }

    /**
     * pistols and derivatives
     */
    [HarmonyPatch(typeof(ASAutoShoot), "AttackOnce")]
    public class bhaptics_OnFireAutoShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASAutoShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_" + Plugin.getHandSide(__instance.ItemID));
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
        }
    }

    /**
     * Single shot weapons (snipers, some bows) arms and vest 
     */
    [HarmonyPatch(typeof(ASSingleShoot), "AttackOnce")]
    public class bhaptics_OnFireSingleShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASSingleShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_" + Plugin.getHandSide(__instance.ItemID));
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
        }
    }

    /**
     * testing for charged attack once (charging vest, charging arm r) 
     */
    [HarmonyPatch(typeof(ASSingleChargeShoot), "ClearChargeAttack")]
    public class bhaptics_OnFireSingleChargeShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASSingleChargeShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("ChargedShotVest_" + Plugin.getHandSide(__instance.ItemID));
            Plugin.tactsuitVr.PlaybackHaptics("ChargedShotArm_" + Plugin.getHandSide(__instance.ItemID));
        }
    }

    /**
     * Charging weapons effect when charging
     */
    [HarmonyPatch(typeof(ASAutoChargeShoot), "ShootCanAttack")]
    public class bhaptics_OnFireAutoChargeShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASAutoChargeShoot __instance, bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            if (__result)
            {
                Plugin.chargeWeaponCanShoot = true;
                //start thread
                Plugin.tactsuitVr.StartChargingWeapon(Plugin.getHandSide(__instance.ItemID));
            }
        }
    }

    /**
     * Charging weapons post charging release
     */
    [HarmonyPatch(typeof(ASAutoChargeShoot), "OnUp")]
    public class bhaptics_OnChargingRelease
    {
        [HarmonyPostfix]
        public static void Postfix(ASAutoChargeShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            if (Plugin.chargeWeaponCanShoot)
            {
                Plugin.chargeWeaponCanShoot = false;
                //stop thread
                Plugin.tactsuitVr.StopChargingWeapon(Plugin.getHandSide(__instance.ItemID));

                Plugin.tactsuitVr.PlaybackHaptics("ChargedShotRelease_" + Plugin.getHandSide(__instance.ItemID));
                Plugin.tactsuitVr.PlaybackHaptics("ChargedShotReleaseArms_" + Plugin.getHandSide(__instance.ItemID));
            }
        }
    }

    /**
     * Continue shoot weapons when activating
     */
    [HarmonyPatch(typeof(ASContinueShoot), "StartBulletSkill")]
    public class bhaptics_OnContinueShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASContinueShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.continueWeaponCanShoot = true;
            //start thread
            Plugin.tactsuitVr.StartContinueWeapon(Plugin.getHandSide(__instance.ItemID));
        }
    }

    /**
     * Continue shoot weapons when stop firing
     */
    [HarmonyPatch(typeof(ASContinueShoot), "EndContinueAttack")]
    public class bhaptics_OnContinueStop
    {
        [HarmonyPostfix]
        public static void Postfix(ASContinueShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            if (Plugin.continueWeaponCanShoot)
            {
                Plugin.continueWeaponCanShoot = false;
                //stop thread
                Plugin.tactsuitVr.StopContinueWeapon(Plugin.getHandSide(__instance.ItemID));
            }
        }
    }

    /**
     * DownUpShoot (Wild hunt) arms and vest 
     * using ASBaseShoot.StartBulletSkill to cover only the wildhunt itemSID == 1306
     */
    [HarmonyPatch(typeof(ASBaseShoot), "StartBulletSkill")]
    public class bhaptics_OnFireDownUpShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASBaseShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            //Plugin.Log.LogMessage("ITEMID " + __instance.ItemID);
            //Plugin.Log.LogMessage("ITEM SID "+__instance.ItemSID);
            if (__instance.ItemSID == 1306)
            {
                Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_" + Plugin.getHandSide(__instance.ItemID));
                Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
            }
        }
    }
    
    // this method will activate feedback only when cloud weaver
    // transitions from 1 sword held in hand (inactive state/entering new
    // zones or switching to cloud weaver) to active state in which the 5
    // cloud weaver swords begin spinng around the wrist,
    // this can only be activated by initiating sword spinning, it will
    // not activate again until cloud weaver is inactive (new zone or switching) 
    [HarmonyPatch(typeof(ASFlyswordShoot), "StartBulletSkill")]
    public class bhaptics_OnFireFlySwordStart
    {
        [HarmonyPostfix]
        public static void Postfix(ASFlyswordShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.StartCloudWeaver(Plugin.getHandSide(__instance.ItemID));
        }
    }

    /**
     * LANCE : Might not be enough and not covering
     * when downed, when ui on (scrolls, pause menu, etc)
     */
    [HarmonyPatch(typeof(ASFlyswordShoot), "Destroy")]
    public class bhaptics_OnFireFlySwordStopHaptics
    {
        [HarmonyPostfix]
        public static void Postfix(ASFlyswordShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.StopCloudWeaver(Plugin.getHandSide(__instance.ItemID));
        }
    }

    // this method will activate feedback only while cloud weaver is
    // actively hitting enemies, does not activate from any button presses,
    // may be ideal to change from flyswordvest and flyswordarmwristspinning
    // to recoil variants
    [HarmonyPatch(typeof(ASFlyswordShoot), "FlyswordOnDown")]
    public class bhaptics_OnFireFlySwordOnDown
    {
        [HarmonyPostfix]
        public static void Postfix(ASFlyswordShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_" + Plugin.getHandSide(__instance.ItemID));
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
        }
    }

    #endregion

    #region Primary skills (furies)

    /**
     * triggering skill on Down
     */
    [HarmonyPatch(typeof(HeroAttackCtrl), "StartActiveSkills")]
    public class bhaptics_OnPrimarySkillOnDown
    {
        public static bool continuousPrimaryStart = false;
        public static int kasuniState = 0;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled || HeroAttackCtrl.CanFireButtonStartCharging)
            {
                return;
            }

            //heroIds switch cases
            switch (HeroAttackCtrl.HeroObj.playerProp.SID)
            {
                //dog
                case 201:
                    Plugin.tactsuitVr.PlaybackHaptics("DogDualWield");
                    break;
                //cat
                case 205:
                    Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillCat");
                    Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillCatVest");
                    break;
                    
                //monkey
                case 214:
                    Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillMonkeyArm");
                    Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillMonkeyVest");
                    break;

                //falcon
                case 206:
                    break;

                //tiger
                case 207:
                    Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillTigerVest", true, 4.0f);
                    Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillTigerArms");
                    break;

                //turtle
                case 213:
                    if (!continuousPrimaryStart)
                    {
                        continuousPrimaryStart = true;
                        //start effect
                        Plugin.tactsuitVr.StartTurtlePrimarySkill();
                        Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillTurtleVest");
                    }
                    break;

                //fox
                case 215:
                    //activation + continuous
                    if (kasuniState == 0)
                    {
                        Plugin.tactsuitVr.StartFoxPrimarySkill();
                        kasuniState = 1;
                        break;
                    }
                    //release
                    if (kasuniState == 1)
                    {
                        //stop effect
                        Plugin.tactsuitVr.StopFoxPrimarySkill();
                        Plugin.tactsuitVr.PlaybackHaptics("PrimaySkillFoxVestRelease");
                        Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillFoxArmsRelease");
                        kasuniState = 0;
                        break;
                    }
                    break;

                //rabbit
                case 212:
                    if (!continuousPrimaryStart)
                    {
                        continuousPrimaryStart = true;
                        //start effect
                        Plugin.tactsuitVr.StartBunnyPrimarySkill();
                    }
                    break;

                default:
                    return;
            }
        }
    }
    
   /**
    * Stop primary skills continuous effects turtle
    */
   [HarmonyPatch(typeof(HeroAttackCtrl), "BreakPower")]
    public class bhaptics_OnSkillBreak
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            bhaptics_OnPrimarySkillOnDown.continuousPrimaryStart = false;
            Plugin.tactsuitVr.StopTurtlePrimarySkill();
            bhaptics_OnPrimarySkillOnDown.kasuniState=0;
            Plugin.tactsuitVr.StopFoxPrimarySkill();
        }
    }

    /**
    * Stop primary skills continuous effects turtle
    */
    [HarmonyPatch(typeof(SkillBolt.Cartoon1200405), "Active")]
    public class bhaptics_OnSkillEnd
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled || HeroAttackCtrl.HeroObj.playerProp.SID != 213)
            {
                return;
            }

            if (bhaptics_OnPrimarySkillOnDown.continuousPrimaryStart)
            {
                bhaptics_OnPrimarySkillOnDown.continuousPrimaryStart = false;
                //stop effect
                Plugin.tactsuitVr.StopTurtlePrimarySkill();
                Plugin.tactsuitVr.PlaybackHaptics("PrimarySkillTurtleVest");
            }
        }
    }


    /**
    * Stop primary skills continuous effects bunny
    */
    [HarmonyPatch(typeof(UIScript.HeroSKillLogicBase), "CommonColdDown")]
    public class bhaptics_OnSkillEndBunny
    {
        [HarmonyPostfix]
        public static void Postfix(UIScript.HeroSKillLogicBase __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || HeroAttackCtrl.HeroObj.playerProp.SID != 212)
            {
                return;
            }

            if (bhaptics_OnPrimarySkillOnDown.continuousPrimaryStart)
            {
                bhaptics_OnPrimarySkillOnDown.continuousPrimaryStart = false;
                //stop effect
                Plugin.tactsuitVr.StopBunnyPrimarySkill();
            }
        }
    }

    /**
     * Secondary skill on Down
     */
    [HarmonyPatch(typeof(HeroAttackCtrl), "ReadyThrowGrenade")]
    public class bhaptics_OnSecondarySkillOnDown
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled || !WarPanelManager.instance.m_canThrowGrenade)
            {
                return;
            }

            //heroIds switch cases
            switch (HeroAttackCtrl.HeroObj.playerProp.SID)
            {                
                //monkey
                case 214:
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillMonkeyVest");
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillMonkeyArm");
                    break;

                //falcon
                case 206:
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillBirdArm");
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillBirdVest");
                    break;

                //tiger
                case 207:
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillTigerArm");
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillTigerVest");
                    break;

                //turtle
                case 213:
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillTurtleArm");
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillTurtleVest");
                    break;

                //rabbit
                case 212:                    
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillBunnyArm");
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillBunnyVest");
                    break;

                default:
                    return;
            }
        }
    }
    
    /**
     * Secondary skill
     */
    [HarmonyPatch(typeof(HeroAttackCtrl), "ThrowGrenade")]
    public class bhaptics_OnSecondarySkillOnUp
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled || !WarPanelManager.instance.m_canThrowGrenade)
            {
                return;
            }

            //heroIds switch cases
            switch (HeroAttackCtrl.HeroObj.playerProp.SID)
            {
                //cat
                case 205:
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillCatVest");
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillCat");
                    break;

                //dog
                case 201:
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillDogVest");
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillCat");
                    break;
                //fox
                case 215:
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillFoxArm");
                    Plugin.tactsuitVr.PlaybackHaptics("SecondarySkillFoxVest");
                    break;

                default:
                    return;
            }
        }
    }

    #endregion

    #region Secondary skills (grenades)

    #endregion

    #region Moves

    /**
    * OnJumping
    */
    [HarmonyPatch(typeof(HeroMoveState.HeroMoveMotor), "OnJump")]
    public class bhaptics_OnJumping
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("OnJump", true, 0.5f);
        }
    }

    /**
     * After jumps when touching floor
     */
    [HarmonyPatch(typeof(HeroMoveManager), "OnLand")]
    public class bhaptics_OnLanding
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("LandAfterJump", true, 0.3f);
        }
    }
    
    /**
     * On Dashing
     */
    [HarmonyPatch(typeof(SkillBolt.CAction1310), "Action")]
    public class bhaptics_OnDashing
    {
        [HarmonyPostfix]
        public static void Postfix(SkillBolt.CSkillBase skill)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if (SkillBolt.CServerArg.IsHeroCtrl(skill))
            {
                Plugin.tactsuitVr.PlaybackHaptics("Dash");
            }
        }
    }

    #endregion

    #region Health and shield

    /**
     * When Shield breaks
     */
    [HarmonyPatch(typeof(BoltBehavior.CAction46), "Action")]
    public class bhaptics_OnShieldBreak
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            Plugin.tactsuitVr.PlaybackHaptics("ShieldBreak");
        }
    }
    
    /**
     * When low health starts
     */
    [HarmonyPatch(typeof(HeroBeHitCtrl), "PlayLowHpAndShield")]
    public class bhaptics_OnLowHealthStart
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (HeroBeHitCtrl.NearlyDeadAction != -1)
            {
                Plugin.tactsuitVr.StartHeartBeat();
            }
        }
    }
    
    /**
     * When low hp stops
     */
    [HarmonyPatch(typeof(HeroBeHitCtrl), "DelLowHpAndShield")]
    public class bhaptics_OnLowHealthStop
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    /**
     * Can't find hit transform object, using static class
     * as an gydrator or factory of some sort in the original code, ugly
     * 
     * Use this function as well for geros with armor instead of shield
     * 
     * Death effect
     */
    [HarmonyPatch(typeof(HeroBeHitCtrl), "HeroInjured")]
    public class bhaptics_OnInjured
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            Plugin.tactsuitVr.PlaybackHaptics("Impact");
            //armor break for heros with armor and no shield
            PlayerProp playerProp = NewObjectCache.GetPlayerProp(HeroBeHitCtrl.HeroID);
            if (playerProp.ArmorMax > 0 &&  playerProp.Armor <= 0)
            {
                Plugin.tactsuitVr.PlaybackHaptics("ShieldBreak");
            }
            //death
            if (playerProp.HP <= 0)
            {
               
                Plugin.tactsuitVr.PlaybackHaptics("Death");
                Plugin.tactsuitVr.StopThreads();
                Plugin.tactsuitVr.StartHeartBeat();
            }
        }
    }

    /**
     * When player gives up after death
     */
    [HarmonyPatch(typeof(UIScript.PCResurgencePanel_Logic), "GiveUp")]
    public class bhaptics_OnGiveUp
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    /**
     * When player is NOT back to life, stop heartbeat
     */
    [HarmonyPatch(typeof(SalvationManager), "AskEnterWatch")]
    public class bhaptics_OnNotRevived
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    /**
     * When player is back to life, stop heartbeat
     */
    [HarmonyPatch(typeof(NewPlayerManager), "PlayerRelife")]
    public class bhaptics_OnBackToLife
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    /**
     * When healing item used
     */
    [HarmonyPatch(typeof(BoltBehavior.CAction1069), "Action")]
    public class bhaptics_OnHealing
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Heal");
        }
    }

    #endregion
}

