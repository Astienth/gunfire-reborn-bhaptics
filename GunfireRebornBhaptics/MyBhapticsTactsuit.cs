using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Bhaptics.Tact;
using UnityEngine;
using GunfireRebornBhaptics;

namespace MyBhapticsTactsuit
{

    public class TactsuitVR
    {
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        // Event to start and stop the heartbeat thread
        private static ManualResetEvent HeartBeat_mrse = new ManualResetEvent(false); 
        private static ManualResetEvent ChargingWeaponR_mrse = new ManualResetEvent(false);
        private static ManualResetEvent ChargingWeaponL_mrse = new ManualResetEvent(false);
        private static ManualResetEvent ContinueWeaponR_mrse = new ManualResetEvent(false);
        private static ManualResetEvent ContinueWeaponL_mrse = new ManualResetEvent(false);
        private static ManualResetEvent CloudWeaverR_mrse = new ManualResetEvent(false);
        private static ManualResetEvent CloudWeaverL_mrse = new ManualResetEvent(false);
        private static ManualResetEvent turtlePrimarySkill_mrse = new ManualResetEvent(false);
        
        // dictionary of all feedback patterns found in the bHaptics directory
        public Dictionary<String, FileInfo> FeedbackMap = new Dictionary<String, FileInfo>();

        public static bool heartbeatStarted = false;

#pragma warning disable CS0618 // remove warning that the C# library is deprecated
        public HapticPlayer hapticPlayer;
#pragma warning restore CS0618 

        private static RotationOption defaultRotationOption = new RotationOption(0.0f, 0.0f);

        public TactsuitVR()
        {

            LOG("Initializing suit");
            try
            {
#pragma warning disable CS0618 // remove warning that the C# library is deprecated
                hapticPlayer = new HapticPlayer("GunfireRebornBhaptics", "GunfireRebornBhaptics");
#pragma warning restore CS0618
                suitDisabled = false;
            }
            catch { LOG("Suit initialization failed!"); }
            RegisterAllTactFiles();
            LOG("Starting HeartBeat thread...");
            Thread HeartBeatThread = new Thread(HeartBeatFunc);
            HeartBeatThread.Start();
            Thread ChargingWeaponRThread = new Thread(ChargingWeaponR);
            ChargingWeaponRThread.Start();
            Thread ChargingWeaponLThread = new Thread(ChargingWeaponL);
            ChargingWeaponLThread.Start();
            Thread ContinueWeaponRThread = new Thread(ContinueWeaponR);
            ContinueWeaponRThread.Start();
            Thread ContinueWeaponLThread = new Thread(ContinueWeaponL);
            ContinueWeaponLThread.Start();
            Thread CloudWeaverRThread = new Thread(CloudWeaverR);
            CloudWeaverRThread.Start();
            Thread CloudWeaverLThread = new Thread(CloudWeaverR);
            CloudWeaverLThread.Start();
            Thread turtlePrimarySkillThread = new Thread(turtlePrimarySkill);
            turtlePrimarySkillThread.Start();
        }

        public void LOG(string logStr)
        {
            Plugin.Log.LogMessage(logStr);
        }


        void RegisterAllTactFiles()
        {
            if (suitDisabled) { return; }
            // Get location of the compiled assembly and search through "bHaptics" directory and contained patterns
            string assemblyFile = Assembly.GetExecutingAssembly().Location;
            string myPath = Path.GetDirectoryName(assemblyFile);
            LOG("Assembly path: " + myPath);
            string configPath = myPath + "\\bHaptics";
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.tact", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                try
                {
                    hapticPlayer.RegisterTactFileStr(prefix, tactFileStr);
                    LOG("Pattern registered: " + prefix);
                }
                catch (Exception e) { LOG(e.ToString()); }

                FeedbackMap.Add(prefix, Files[i]);
            }
            systemInitialized = true;
        }

        public void PlaybackHaptics(String key, bool forced = true, float intensity = 1.0f, float duration = 1.0f)
        {
            if (suitDisabled) { return; }
            if (FeedbackMap.ContainsKey(key))
            {
                ScaleOption scaleOption = new ScaleOption(intensity, duration);
                if (hapticPlayer.IsPlaying(key) && !forced)
                {
                    return;
                }
                else
                {
                    hapticPlayer.SubmitRegisteredVestRotation(key, key, defaultRotationOption, scaleOption);
                }
            }
            else
            {
                LOG("Feedback not registered: " + key);
            }
        }

        public void HeartBeatFunc()
        {
            while (true)
            {
                // Check if reset event is active
                HeartBeat_mrse.WaitOne();
                PlaybackHaptics("HeartBeat", false, 1.5f);
                Thread.Sleep(1000);
            }
        }

        public void StartHeartBeat()
        {
            if (!heartbeatStarted)
            {
                heartbeatStarted = true;
                HeartBeat_mrse.Set();
            }
        }

        public void StopHeartBeat()
        {
            heartbeatStarted = false;
            HeartBeat_mrse.Reset();
        }

        // CLOUD WEAVER WEAPON
        public void CloudWeaverR()
        {
            while (true)
            {
                // Check if reset event is active
                CloudWeaverR_mrse.WaitOne();
                PlaybackHaptics("FlySwordVest_R");
                PlaybackHaptics("FlySwordArmRWristSpinning_R");
                Thread.Sleep(1000);
            }
        }
        public void CloudWeaverL()
        {
            while (true)
            {
                // Check if reset event is active
                CloudWeaverR_mrse.WaitOne();
                PlaybackHaptics("FlySwordVest_L");
                PlaybackHaptics("FlySwordArmRWristSpinning_L");
                Thread.Sleep(1000);
            }
        }

        public void StartCloudWeaver(string side = "R")
        {
            if (side == "R")
            {
                CloudWeaverR_mrse.Set();
            }
            else
            {
                CloudWeaverL_mrse.Set();
            }
        }

        public void StopCloudWeaver(string side = "R")
        {
            if (side == "R")
            {
                CloudWeaverR_mrse.Reset();
            }
            else
            {
                CloudWeaverL_mrse.Reset();
            }
        }

        //CHARGING WEAPONS FUNCTIONS
        public void ChargingWeaponR()
        {
            while (true)
            {
                // Check if reset event is active
                ChargingWeaponR_mrse.WaitOne();
                PlaybackHaptics("ChargedShotVest_R" , true, 0.3f);
                PlaybackHaptics("ChargedShotArm_R" , true, 0.4f);
                Thread.Sleep(1000); 
            }
        }

        public void ChargingWeaponL()
        {
            while (true)
            {
                // Check if reset event is active
                ChargingWeaponL_mrse.WaitOne();
                PlaybackHaptics("ChargedShotVest_L", true, 0.3f);
                PlaybackHaptics("ChargedShotArm_L", true, 0.4f);
                Thread.Sleep(1000);
            }
        }

        public void StartChargingWeapon(string side = "R")
        {
            if (side == "R")
            {
                ChargingWeaponR_mrse.Set();
            }
            else
            {
                ChargingWeaponL_mrse.Set();
            }
        }

        public void StopChargingWeapon(string side = "R")
        {
            if (side == "R")
            {
                ChargingWeaponR_mrse.Reset();
            }
            else
            {
                ChargingWeaponL_mrse.Reset();
            }
        }

        // CONTINUOURS WEAPONS FUNCTIONS
        public void ContinueWeaponR()
        {
            while (true)
            {
                // Check if reset event is active
                ContinueWeaponR_mrse.WaitOne();
                PlaybackHaptics("ContinuousVest_R");
                PlaybackHaptics("ContinuousArm_R");
                Thread.Sleep(400);
            }
        }

        public void ContinueWeaponL()
        {
            while (true)
            {
                // Check if reset event is active
                ContinueWeaponL_mrse.WaitOne();
                PlaybackHaptics("ContinuousVest_L");
                PlaybackHaptics("ContinuousArm_L");
                Thread.Sleep(400);
            }
        }

        public void StartContinueWeapon(string side = "R")
        {
            if (side == "R")
            {
                ContinueWeaponR_mrse.Set();
            }
            else
            {
                ContinueWeaponL_mrse.Set();
            }
        }

        public void StopContinueWeapon(string side = "R")
        {
            if (side == "R")
            {
                ContinueWeaponR_mrse.Reset();
            }
            else
            {
                ContinueWeaponL_mrse.Reset();
            }
        }


        public void turtlePrimarySkill()
        {
            while (true)
            {
                // Check if reset event is active
                turtlePrimarySkill_mrse.WaitOne();
                PlaybackHaptics("PrimarySkillTurtleArm_R");
                Thread.Sleep(1000);
            }
        }

        public void StartTurtlePrimarySkill()
        {
            turtlePrimarySkill_mrse.Set();
        }

        public void StopTurtlePrimarySkill()
        {
            turtlePrimarySkill_mrse.Reset();
        }

        public void StopHapticFeedback(String effect)
        {
            hapticPlayer.TurnOff(effect);
        }

        public void StopAllHapticFeedback()
        {
            StopThreads();
            foreach (String key in FeedbackMap.Keys)
            {
                hapticPlayer.TurnOff(key);
            }
        }

        public void StopThreads()
        {
            StopHeartBeat();
            ChargingWeaponL_mrse.Reset();
            ChargingWeaponR_mrse.Reset();
            ContinueWeaponL_mrse.Reset();
            ContinueWeaponR_mrse.Reset();
            CloudWeaverR_mrse.Reset();
            CloudWeaverL_mrse.Reset();
            turtlePrimarySkill_mrse.Reset();
        }


    }
}
