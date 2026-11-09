#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using KrakenCache.Data;

namespace KrakenCache.Editor
{
    [InitializeOnLoad]
    public static class GameAssetsAutoBuilder
    {
        private const string AssetPath = "Assets/_Project/Resources/GameAssets.asset";

        static GameAssetsAutoBuilder()
        {
            EditorApplication.delayCall += EnsureAssetsBuilt;
        }

        [MenuItem("Kraken Cache/Rebuild Asset Registry", false, 20)]
        public static void EnsureAssetsBuilt()
        {
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetPath);
            if (assets == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                }

                assets = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(assets, AssetPath);
            }

            // Assign Cubes
            assets.redCube = LoadSprite("Assets/KrakenArt/Cubes/DefaultState/red.png");
            assets.greenCube = LoadSprite("Assets/KrakenArt/Cubes/DefaultState/green.png");
            assets.blueCube = LoadSprite("Assets/KrakenArt/Cubes/DefaultState/blue.png");
            assets.yellowCube = LoadSprite("Assets/KrakenArt/Cubes/DefaultState/yellow.png");

            // Assign Rocket Hints
            assets.redRocketHint = LoadSprite("Assets/KrakenArt/Cubes/RocketState/red_rocket.png");
            assets.greenRocketHint = LoadSprite("Assets/KrakenArt/Cubes/RocketState/green_rocket.png");
            assets.blueRocketHint = LoadSprite("Assets/KrakenArt/Cubes/RocketState/blue_rocket.png");
            assets.yellowRocketHint = LoadSprite("Assets/KrakenArt/Cubes/RocketState/yellow_rocket.png");

            // Assign TNT Hints
            assets.redTntHint = LoadSprite("Assets/KrakenArt/Cubes/TntState/red_tnt.png");
            assets.greenTntHint = LoadSprite("Assets/KrakenArt/Cubes/TntState/green_tnt.png");
            assets.blueTntHint = LoadSprite("Assets/KrakenArt/Cubes/TntState/blue_tnt.png");
            assets.yellowTntHint = LoadSprite("Assets/KrakenArt/Cubes/TntState/yellow_tnt.png");

            // Assign Special Items
            assets.horizontalRocket = LoadSprite("Assets/KrakenArt/SpecialItems/Rocket/horizontal_rocket.png");
            assets.horizontalRocketPartLeft = LoadSprite("Assets/KrakenArt/SpecialItems/Rocket/horizontal_rocket_part_left.png");
            assets.horizontalRocketPartRight = LoadSprite("Assets/KrakenArt/SpecialItems/Rocket/horizontal_rocket_part_right.png");
            assets.verticalRocket = LoadSprite("Assets/KrakenArt/SpecialItems/Rocket/vertical_rocket.png");
            assets.verticalRocketPartBottom = LoadSprite("Assets/KrakenArt/SpecialItems/Rocket/vertical_rocket_part_bottom.png");
            assets.verticalRocketPartTop = LoadSprite("Assets/KrakenArt/SpecialItems/Rocket/vertical_rocket_part_top.png");
            assets.tnt = LoadSprite("Assets/KrakenArt/SpecialItems/TNT/TNT.png");

            // Assign Obstacles
            assets.stone = LoadSprite("Assets/KrakenArt/Obstacles/Stone/stone.png");
            assets.vaseFull = LoadSprite("Assets/KrakenArt/Obstacles/Vase/vase_01.png");
            assets.vaseDamaged = LoadSprite("Assets/KrakenArt/Obstacles/Vase/vase_02.png");
            assets.chaliceBoxBg = LoadSprite("Assets/KrakenArt/Obstacles/ChaliceBox/ChaliceBoxBg.png");
            assets.chaliceBoxDoors = LoadSprite("Assets/KrakenArt/Obstacles/ChaliceBox/ChaliceBoxDoors.png");
            assets.chalice = LoadSprite("Assets/KrakenArt/Obstacles/ChaliceBox/Chalice.png");

            // Assign Board & UI
            assets.gridBackground = LoadSprite("Assets/KrakenArt/UI/Gameplay/grid_background.png");
            assets.topUi = LoadSprite("Assets/KrakenArt/UI/Gameplay/Top/top_ui.png");
            assets.goalCheck = LoadSprite("Assets/KrakenArt/UI/Gameplay/Top/goal_check.png");
            assets.celebrationStar = LoadSprite("Assets/KrakenArt/UI/Gameplay/Celebration/star.png");
            assets.popupBase = LoadSprite("Assets/KrakenArt/UI/Gameplay/Popup/popup_base.png");
            assets.popupRibbon = LoadSprite("Assets/KrakenArt/UI/Gameplay/Popup/popup_ribbon.png");
            assets.closeButton = LoadSprite("Assets/KrakenArt/UI/Gameplay/Popup/close_button.png");
            assets.menuButton = LoadSprite("Assets/KrakenArt/UI/Menu/button.png");
            assets.menuButtonFrame = LoadSprite("Assets/KrakenArt/UI/Menu/button_frame.png");
            assets.menuBackground = LoadSprite("Assets/KrakenArt/Menu/background.png");

            assets.particleRed = LoadSprite("Assets/KrakenArt/Cubes/Particles/particle_red.png");
            assets.particleGreen = LoadSprite("Assets/KrakenArt/Cubes/Particles/particle_green.png");
            assets.particleBlue = LoadSprite("Assets/KrakenArt/Cubes/Particles/particle_blue.png");
            assets.particleYellow = LoadSprite("Assets/KrakenArt/Cubes/Particles/particle_yellow.png");
            assets.particleSmoke = LoadSprite("Assets/KrakenArt/SpecialItems/Rocket/Particles/particle_smoke.png");
            assets.particleStar = LoadSprite("Assets/KrakenArt/UI/Gameplay/Celebration/Particles/additive_particle_star.png");
            assets.stoneParticles = LoadParticles("Assets/KrakenArt/Obstacles/Stone/Particles");
            assets.vaseParticles = LoadParticles("Assets/KrakenArt/Obstacles/Vase/Particles");
            assets.doorParticles = LoadParticles("Assets/KrakenArt/Obstacles/ChaliceBox/Particles/DoorParticles");
            assets.boxParticles = LoadParticles("Assets/KrakenArt/Obstacles/ChaliceBox/Particles/BaseParticles");
            assets.tntParticles = LoadParticles("Assets/KrakenArt/SpecialItems/TNT/Particles");

            const string materialPath = "Assets/_Project/Resources/ParticleStar.mat";
            assets.starMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (assets.starMaterial == null)
            {
                assets.starMaterial = new Material(Shader.Find("KrakenCache/AdditiveSprite"));
                AssetDatabase.CreateAsset(assets.starMaterial, materialPath);
            }
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("[GameAssetsAutoBuilder] Successfully bound all GameAssets sprites!");
        }

        private static Sprite[] LoadParticles(string folder)
        {
            var ids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            System.Array.Sort(ids, System.StringComparer.Ordinal);
            var sprites = new Sprite[ids.Length];
            for (int i = 0; i < ids.Length; i++) sprites[i] = LoadSprite(AssetDatabase.GUIDToAssetPath(ids[i]));
            return sprites;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[GameAssetsAutoBuilder] Sprite not found at: {path}");
            }
            return sprite;
        }
    }
}
#endif
