using System.Collections.Generic;
using UnityEngine;

namespace NicolasMassara.CustomUpdateManager
{
    public static class CustomTime
    {
        private static readonly Dictionary<UpdateGroup, TimeChannel> Channels = new();

        public static float GlobalTimeScale = 1;
        public static float GlobalFixedTimeScale = 1;
        
        public static TimeChannel GetChannel(UpdateGroup key)
        {
            if (!Channels.ContainsKey(key))
                Channels[key] = new TimeChannel();
            return Channels[key];
        }

        public static bool GetIsPausedByChannel(UpdateGroup key)
        {
            return GetChannel(key).IsPaused;
        }

        public static float GetDeltaTimeByChannel(UpdateGroup key)
        {
            return GetChannel(key).DeltaTime;
        }
        
        public static float GetUnscaledDeltaTimeByChannel(UpdateGroup key)
        {
            return GetChannel(key).UnscaledDeltaTime;
        }
        
        public static float GetFixedDeltaTimeByChannel(UpdateGroup key)
        {
            return GetChannel(key).FixedDeltaTime;
        }
        
        public static float GetUnscaledFixedDeltaTimeByChannel(UpdateGroup key)
        {
            return GetChannel(key).UnscaledFixedDeltaTime;
        }

        internal static void UpdateAll(float unscaledDeltaTime)
        {
            foreach (var kv in Channels)
                kv.Value.Update(unscaledDeltaTime * GlobalTimeScale);
        }
        
        internal static void FixedUpdateAll(float unscaledDeltaTime)
        {
            foreach (var kv in Channels)
                kv.Value.UpdateFixed(unscaledDeltaTime * GlobalFixedTimeScale);
        }

        public static void SetChannelPaused(UpdateGroup updateGroup, bool isPaused)
        {
            if (updateGroup == UpdateGroup.Always)
            {
                Debug.LogWarning("Update Group: Always cannot be paused");
                return;
            }

            GetChannel(updateGroup).SetPaused(isPaused);
        }
        
        public static void SetChannelPaused(UpdateGroup[] updateGroup, bool isPaused)
        {
            for (int i = 0; i < updateGroup.Length; i++)
            {
                GetChannel(updateGroup[i]).SetPaused(isPaused);
            }
        }
        
        public static void SetChannelTimeScale(UpdateGroup updateGroup, float timeScale)
        {
            if (updateGroup == UpdateGroup.Always)
            {
                Debug.LogWarning("Update Group: Always cannot be modified");
                return;
            }
            
            GetChannel(updateGroup).SetTimeScale(timeScale);
        }

        public static void SetChannelTimeScale(UpdateGroup[] updateGroup, float timeScale)
        {
            for (int i = 0; i < updateGroup.Length; i++)
            {
                GetChannel(updateGroup[i]).SetTimeScale(timeScale);
            }
        }
    }

    public class TimeChannel
    {
        public float DeltaTime { get; private set; }
        public float UnscaledDeltaTime { get; private set; }
        public float FixedDeltaTime { get; private set; }
        public float UnscaledFixedDeltaTime { get; private set; }
        public float TimeScale = 1f;
        public bool IsPaused = false;
        
        public void SetTimeScale(float scale) => TimeScale = Mathf.Max(0f, scale);
        public void SetPaused(bool value) => IsPaused = value;

        public void Update(float unscaledDeltaTime)
        {
            UnscaledDeltaTime =  IsPaused ? 0f : unscaledDeltaTime;
            DeltaTime = IsPaused ? 0f : unscaledDeltaTime * TimeScale;
        }
        
        public void UpdateFixed(float fixedUnscaledDeltaTime)
        {
            UnscaledFixedDeltaTime = IsPaused ? 0f :  fixedUnscaledDeltaTime;
            FixedDeltaTime = IsPaused ? 0f : fixedUnscaledDeltaTime * TimeScale;
        }
    }
}