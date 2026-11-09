using UnityEngine;

namespace KrakenCache.Data
{
    public static class LevelLoader
    {
        public const int MaxLevelCount = 10;

        public static LevelData LoadLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > MaxLevelCount)
            {
                Debug.LogError($"[LevelLoader] Invalid level requested: {levelNumber}. Must be 1 to {MaxLevelCount}.");
                return null;
            }

            string resourcePath = $"Levels/level_{levelNumber:D2}";
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

            if (jsonAsset == null)
            {
                Debug.LogError($"[LevelLoader] Level asset not found at Resources/{resourcePath}");
                return null;
            }

            try
            {
                LevelData data = JsonUtility.FromJson<LevelData>(jsonAsset.text);
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelLoader] Error parsing level {levelNumber} JSON: {ex.Message}");
                return null;
            }
        }
    }
}
