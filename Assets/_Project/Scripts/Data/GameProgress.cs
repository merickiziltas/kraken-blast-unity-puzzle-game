using System;
using UnityEngine;

namespace KrakenCache.Data
{
    public static class GameProgress
    {
        private const string LevelPrefKey = "KC_Expedition";
        public const int MaxLevel = 10;
        private static int? selectedLevel;

        public static event Action<int> OnLevelChanged;

        public static int LastPlayedLevel
        {
            get => PlayerPrefs.GetInt(LevelPrefKey, 1);
            set
            {
                selectedLevel = null;
                SaveProgress(Mathf.Clamp(value, 1, MaxLevel + 1));
                OnLevelChanged?.Invoke(CurrentLevel);
            }
        }

        public static bool IsAllLevelsFinished => LastPlayedLevel > MaxLevel;
        public static int CurrentLevel => selectedLevel ?? LastPlayedLevel;

        public static void SelectLevel(int level)
        {
            selectedLevel = Mathf.Clamp(level, 1, MaxLevel);
            OnLevelChanged?.Invoke(CurrentLevel);
        }

        public static void CompleteCurrentLevel()
        {
            int completedLevel = CurrentLevel;
            if (completedLevel > MaxLevel) return;

            // A chosen expedition can continue in this session without skipping
            // unbeaten levels or erasing the saved campaign.
            if (selectedLevel.HasValue)
                selectedLevel = completedLevel < MaxLevel ? completedLevel + 1 : (int?)null;

            if (completedLevel == LastPlayedLevel)
                SaveProgress(completedLevel + 1);

            OnLevelChanged?.Invoke(CurrentLevel);
        }

        private static void SaveProgress(int level)
        {
            PlayerPrefs.SetInt(LevelPrefKey, level);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            LastPlayedLevel = 1;
        }
    }
}
