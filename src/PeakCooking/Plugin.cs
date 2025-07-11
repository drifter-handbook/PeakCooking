using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PEAKLib.Core;
using PEAKLib.Items;
using System.IO;
using UnityEngine;

namespace PeakCooking;

[BepInAutoPlugin]
[BepInDependency("com.github.PEAKModding.PEAKLib.Core", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.github.PEAKModding.PEAKLib.Items", BepInDependency.DependencyFlags.HardDependency)]
public partial class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    internal static AssetBundle Bundle { get; set; } = null!;
    internal static ModDefinition Definition { get; set; } = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        Definition = ModDefinition.GetOrCreate(Info.Metadata);

        string AssetBundlePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "peak_cooking");
        Bundle = AssetBundle.LoadFromFile(AssetBundlePath);

        GameObject cookingPotPrefab = Bundle.LoadAsset<GameObject>("Assets/Modding/CookingPot.prefab");
        cookingPotPrefab.AddComponent<CookingPot>();
        new ItemContent(cookingPotPrefab.GetComponent<Item>()).Register(Definition);

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
    }
}
