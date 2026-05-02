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
        public static AssetBundle ab;
        public static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                Transform result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
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

            ab = AssetBundle.LoadFromMemory(buffer);
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
            if (!__instance.transform.root.TryGetComponent<NewMovement>(out _)) {
                Plugin.Logger.LogInfo("Mrow!! o(>W<)o");
                Transform easter_ref = __instance.transform.Find("Easter");
                Transform ears = GameObject.Instantiate(Plugin.ab.LoadAsset<GameObject>("necoears"), __instance.transform).transform;
                if (!__instance.transform.root.TryGetComponent<PlatformerMovement>(out _))
                    ears.localPosition = easter_ref.localPosition;
                else
                    ears.localPosition = new Vector3(-0f,  0.2f, -0.25f);
                ears.localScale = easter_ref.localScale;
                ears.localRotation = easter_ref.localRotation;
            }
        }
    }
}