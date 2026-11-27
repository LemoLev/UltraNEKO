using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ULTRANEKO
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        internal static new ManualLogSource Logger;
        public static GameObject ears;
        private static bool shadersChecked;

        // The embedded bundle carries its own copy of "Unlit/Color" compiled for
        // Direct3D 11 only. When the game runs on OpenGL or Vulkan (native Linux,
        // -force-glcore, -force-vulkan) that copy can't be used and the ears
        // render pink or not at all. Swap in a shader the game can render.
        internal static void FixUnsupportedShaders()
        {
            if (shadersChecked || ears == null) return;
            bool allResolved = true;
            foreach (Renderer renderer in ears.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || material.shader.isSupported) continue;
                    string original = material.shader.name;

                    Shader same = FindSupportedShader(original);
                    if (same != null)
                    {
                        material.shader = same;
                        Logger.LogInfo($"Using the game's '{original}' shader for '{material.name}'");
                        continue;
                    }

                    // The game ships Unlit/Color but rarely keeps it loaded.
                    // Unlit/Texture with a one-pixel texture of the same colour
                    // renders identically (unlit, opaque, fog) and is always available.
                    Shader unlitTexture = original == "Unlit/Color" ? FindSupportedShader("Unlit/Texture") : null;
                    if (unlitTexture != null)
                    {
                        Color color = ReadSavedColor(material);
                        Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                        pixel.SetPixel(0, 0, color);
                        pixel.Apply();
                        material.shader = unlitTexture;
                        material.mainTexture = pixel;
                        Logger.LogInfo($"Using 'Unlit/Texture' in place of '{original}' for '{material.name}' ({color})");
                        continue;
                    }

                    allResolved = false;
                }
            }
            // If nothing suitable was loaded yet, try again on the next spawn.
            shadersChecked = allResolved;
        }

        // While a material's shader is unusable Unity hides its properties
        // (GetColor returns black). A copy given any shader that declares
        // _Color gets the saved value back.
        private static Color ReadSavedColor(Material material)
        {
            Shader probeShader = FindSupportedShader("Standard");
            if (probeShader == null) return Color.white;
            Material probe = new Material(material) { shader = probeShader };
            Color color = probe.GetColor("_Color");
            Object.Destroy(probe);
            return color;
        }

        private static Shader FindSupportedShader(string name)
        {
            Shader shader = Shader.Find(name);
            if (shader != null && shader.isSupported) return shader;
            foreach (Shader candidate in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (candidate.name == name && candidate.isSupported) return candidate;
            }
            return null;
        }

        private void LoadEmbeddedAssetBundle()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string resourceName = "ULTRANEKO.nekoears.bundle";

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Logger.LogError($"Resource '{resourceName}' not found!");
                return;
            }

            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            AssetBundle ab = AssetBundle.LoadFromMemory(buffer);
            ears = ab.LoadAsset<GameObject>("necoears");
            FixUnsupportedShaders();
        }
        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            LoadEmbeddedAssetBundle();
            new Harmony("mods.mrshadrib.uk.ULTRANEKO").PatchAll();
            Logger.LogInfo($"Nya! :3");
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SeasonalHats), "Start")]
        public static void AttachEars(SeasonalHats __instance)
        {
            Plugin.Logger.LogInfo("Mrr~ X3");
            Plugin.FixUnsupportedShaders();
            Transform easter_ref = __instance.transform.Find("Easter");
            Transform ears = GameObject.Instantiate(Plugin.ears, __instance.transform).transform;
            ears.localPosition = easter_ref.localPosition;
            ears.localScale = easter_ref.localScale;
            ears.localRotation = easter_ref.localRotation;
            ears.GetChild(0).gameObject.layer = __instance.gameObject.layer;
        }
    }
}