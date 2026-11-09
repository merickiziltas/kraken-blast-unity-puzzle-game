using System;
using UnityEngine;

namespace KrakenCache.Data
{
    public static class GameProgress
    {
        private const string LevelPrefKey = "KC_Expedition";
        public const int MaxLevel = 10;

        public static event Action<int> OnLevelChanged;

        public static int LastPlayedLevel
        {
            get => PlayerPrefs.GetInt(LevelPrefKey, 1);
            set
            {
                int clamped = Mathf.Clamp(value, 1, MaxLevel + 1);
                PlayerPrefs.SetInt(LevelPrefKey, clamped);
                PlayerPrefs.Save();
                OnLevelChanged?.Invoke(clamped);
            }
        }

        public static bool IsAllLevelsFinished => LastPlayedLevel > MaxLevel;

        public static void CompleteCurrentLevel()
        {
            if (LastPlayedLevel <= MaxLevel)
            {
                LastPlayedLevel++;
            }
        }

        public static void ResetProgress()
        {
            LastPlayedLevel = 1;
        }
    }
}
