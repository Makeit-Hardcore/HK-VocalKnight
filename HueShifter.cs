using System;
using Modding;
using System.Collections.Generic;
using Satchel.BetterMenus;
using UnityEngine;
using Random = UnityEngine.Random;
using UObject = UnityEngine.Object;

namespace VocalKnight
{
    public class HueShifter
    {
        public static Shader RainbowDefault;
        public static Shader RainbowScreenBlend;
        public static Shader RainbowLit;
        public static Shader RainbowParticleAdd;
        public static Shader RainbowParticleAddSoft;

        public static float Phase = 0; //0f to 360f
        public static RandomPhaseSetting RandomPhase = RandomPhaseSetting.RandomPerMapArea;
        public static bool RespectLighting = true;
        public static float XFrequency = 0; //-100 to 100
        public static float YFrequency = 0; //-100 to 100
        public static float ZFrequency = 0; //-100 to 100
        public static float TimeFrequency = 0; //-100 to 100
        public static bool AllowVanillaPhase = true;

        public static Dictionary<string, float> Palette = new();

        // Rider did this; it's more efficient or something
        private static readonly int PhaseProperty = Shader.PropertyToID("_Phase");
        private static readonly int FrequencyProperty = Shader.PropertyToID("_Frequency");

        public static void LoadAssets()
        {
            RainbowDefault = VocalKnight.HueShiftBundle.LoadAsset<Shader>("assets/shader/rainbowdefault.shader");
            RainbowScreenBlend = VocalKnight.HueShiftBundle.LoadAsset<Shader>("assets/shader/rainbowscreenblend.shader");
            RainbowLit = VocalKnight.HueShiftBundle.LoadAsset<Shader>("assets/shader/rainbowlit.shader");
            RainbowParticleAdd = VocalKnight.HueShiftBundle.LoadAsset<Shader>("assets/shader/rainbowparticleadd.shader");
            RainbowParticleAddSoft = VocalKnight.HueShiftBundle.LoadAsset<Shader>("assets/shader/rainbowparticleaddsoft.shader");
        }

        public static float GetPhase()
        {
            string location;
            switch (RandomPhase)
            {
                case RandomPhaseSetting.RandomPerMapArea:
                    location = GameManager.instance.sm.mapZone.ToString();
                    break;
                case RandomPhaseSetting.RandomPerRoom:
                    location = GameManager.instance.sceneName;
                    break;
                case RandomPhaseSetting.Fixed:
                default:
                    return Phase / 360;
            }

            if (!Palette.ContainsKey(location))
                Palette[location] =
                    AllowVanillaPhase ? Random.Range(0f, 1f) : Random.Range(0.05f, 0.95f);
            return Palette[location];
        }

        public static void SetAllTheShaders()
        {
            var props = new MaterialPropertyBlock();
            var frequencyVector = new Vector4(XFrequency / 40, YFrequency / 40, ZFrequency / 200, TimeFrequency / 10);

            foreach (var renderer in UObject.FindObjectsOfType<Renderer>(true))
            {
                if (GameManager.GetBaseSceneName(renderer.gameObject.scene.name) != GameManager.instance.sceneName) continue;
                if (renderer.gameObject.name == "Item Sprite") continue;

                foreach (var material in renderer.materials)
                {
                    material.shader = material.shader.name switch
                    {
                        "Sprites/Lit" => RespectLighting ? RainbowLit : RainbowDefault,
                        "Sprites/Default" => RainbowDefault,
                        "Sprites/Cherry-Default" => RainbowDefault,
                        "UI/BlendModes/Screen" => RainbowScreenBlend,
                        "Legacy Shaders/Particles/Additive" => RainbowParticleAdd,
                        "Legacy Shaders/Particles/Additive (Soft)" => RainbowParticleAddSoft,
                        _ => material.shader
                    };

                    if (material.shader.name is not (
                        "Custom/RainbowLit" or
                        "Custom/RainbowDefault" or
                        "Custom/RainbowScreenBlend" or
                        "Custom/RainbowParticleAdd" or
                        "Custom/RainbowParticleAddSoft")) continue;
                    renderer.GetPropertyBlock(props);
                    props.SetFloat(PhaseProperty, GetPhase());
                    props.SetVector(FrequencyProperty, frequencyVector);
                    renderer.SetPropertyBlock(props);
                }
            }
        }

        public static void SetDefaults()
        {
            Phase = 0;
            RandomPhase = RandomPhaseSetting.RandomPerMapArea;
            RespectLighting = true;
            XFrequency = 0;
            YFrequency = 0;
            ZFrequency = 0;
            TimeFrequency = 0;
            AllowVanillaPhase = true;

            SetAllTheShaders();
        }
    }

    public enum RandomPhaseSetting
    {
        Fixed,
        RandomPerMapArea,
        RandomPerRoom,
    }
}