using System;
using UnityEngine;
using KrakenCache.Core;

namespace KrakenCache.Data
{
    [CreateAssetMenu(fileName = "GameAssets", menuName = "Kraken Cache/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        [Header("Cube Sprites - Default")]
        public Sprite redCube;
        public Sprite greenCube;
        public Sprite blueCube;
        public Sprite yellowCube;

        [Header("Cube Sprites - Rocket Hint")]
        public Sprite redRocketHint;
        public Sprite greenRocketHint;
        public Sprite blueRocketHint;
        public Sprite yellowRocketHint;

        [Header("Cube Sprites - TNT Hint")]
        public Sprite redTntHint;
        public Sprite greenTntHint;
        public Sprite blueTntHint;
        public Sprite yellowTntHint;

        [Header("Special Items")]
        public Sprite horizontalRocket;
        public Sprite horizontalRocketPartLeft;
        public Sprite horizontalRocketPartRight;
        public Sprite verticalRocket;
        public Sprite verticalRocketPartBottom;
        public Sprite verticalRocketPartTop;
        public Sprite tnt;

        [Header("Obstacles")]
        public Sprite stone;
        public Sprite vaseFull;
        public Sprite vaseDamaged;
        public Sprite chaliceBoxBg;
        public Sprite chaliceBoxDoors;
        public Sprite chalice;

        [Header("Board & UI")]
        public Sprite gridBackground;
        public Sprite topUi;
        public Sprite goalCheck;
        public Sprite celebrationStar;
        public Sprite popupBase;
        public Sprite popupRibbon;
        public Sprite closeButton;
        public Sprite menuButton;
        public Sprite menuButtonFrame;
        public Sprite menuBackground;

        [Header("Particles")]
        public Sprite particleRed, particleGreen, particleBlue, particleYellow;
        public Sprite particleSmoke, particleStar;
        public Material starMaterial;
        public Sprite[] stoneParticles, vaseParticles, doorParticles, boxParticles, tntParticles;

        public Sprite GetCubeParticle(CubeColor color) => color switch
        {
            CubeColor.Red => particleRed,
            CubeColor.Green => particleGreen,
            CubeColor.Blue => particleBlue,
            _ => particleYellow
        };

        public Sprite GetCubeSprite(CubeColor color, HintType hint)
        {
            switch (hint)
            {
                case HintType.Rocket:
                    return color switch
                    {
                        CubeColor.Red => redRocketHint,
                        CubeColor.Green => greenRocketHint,
                        CubeColor.Blue => blueRocketHint,
                        CubeColor.Yellow => yellowRocketHint,
                        _ => null
                    };

                case HintType.TNT:
                    return color switch
                    {
                        CubeColor.Red => redTntHint,
                        CubeColor.Green => greenTntHint,
                        CubeColor.Blue => blueTntHint,
                        CubeColor.Yellow => yellowTntHint,
                        _ => null
                    };

                default:
                    return color switch
                    {
                        CubeColor.Red => redCube,
                        CubeColor.Green => greenCube,
                        CubeColor.Blue => blueCube,
                        CubeColor.Yellow => yellowCube,
                        _ => null
                    };
            }
        }
    }
}
