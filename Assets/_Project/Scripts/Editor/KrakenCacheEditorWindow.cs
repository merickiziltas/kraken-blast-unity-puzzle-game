#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using KrakenCache.Data;

namespace KrakenCache.Editor
{
    public class KrakenCacheEditorWindow : EditorWindow
    {
        private int selectedLevel = 1;

        [MenuItem("Kraken Cache/Set Level...", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<KrakenCacheEditorWindow>("Kraken Level Tool");
            window.minSize = new Vector2(300, 240);
            window.Show();
        }

        private void OnEnable()
        {
            selectedLevel = GameProgress.LastPlayedLevel;
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Kraken Cache - Level Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Current Persisted Level: {(GameProgress.IsAllLevelsFinished ? "All Finished (11)" : $"Level {GameProgress.LastPlayedLevel}")}",
                MessageType.Info);

            GUILayout.Space(10);
            selectedLevel = EditorGUILayout.IntSlider("Select Level", selectedLevel, 1, GameProgress.MaxLevel + 1);

            GUILayout.Space(10);
            if (GUILayout.Button("Apply Selected Level", GUILayout.Height(30)))
            {
                GameProgress.LastPlayedLevel = selectedLevel;
                Debug.Log($"[KrakenCache Tool] Last played level set to: {selectedLevel}");
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Level 1"))
            {
                GameProgress.ResetProgress();
                selectedLevel = 1;
                Debug.Log("[KrakenCache Tool] Progress reset to Level 1.");
            }

            if (GUILayout.Button("Set All Finished"))
            {
                GameProgress.LastPlayedLevel = GameProgress.MaxLevel + 1;
                selectedLevel = GameProgress.MaxLevel + 1;
                Debug.Log("[KrakenCache Tool] Progress set to Finished.");
            }
            EditorGUILayout.EndHorizontal();
        }

        [MenuItem("Kraken Cache/Quick Set/Level 1")]
        private static void SetLevel1() => GameProgress.LastPlayedLevel = 1;

        [MenuItem("Kraken Cache/Quick Set/Level 5")]
        private static void SetLevel5() => GameProgress.LastPlayedLevel = 5;

        [MenuItem("Kraken Cache/Quick Set/Level 10")]
        private static void SetLevel10() => GameProgress.LastPlayedLevel = 10;

        [MenuItem("Kraken Cache/Quick Set/Finished")]
        private static void SetFinished() => GameProgress.LastPlayedLevel = GameProgress.MaxLevel + 1;
    }
}
#endif
