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
        }
        private void Awake()
        {
            // Plugin startup logic
            LoadEmbeddedAssetBundle();
            new Harmony("mods.mrshadrib.uk.ULTRANEKO").PatchAll();
            Logger = base.Logger;
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
            Transform easter_ref = __instance.transform.Find("Easter");
            Transform ears = GameObject.Instantiate(Plugin.ears, __instance.transform).transform;
            ears.localPosition = easter_ref.localPosition;
            ears.localScale = easter_ref.localScale;
            ears.localRotation = easter_ref.localRotation;
            ears.GetChild(0).gameObject.layer = __instance.gameObject.layer;
        }
    }
}