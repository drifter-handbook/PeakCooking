using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PEAKLib.Core;
using PEAKLib.Items;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zorro.Core;

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

    // Single configuration option - enable/disable the mod
    private ConfigEntry<bool>? _placeAtSpawn;

    private bool _gameStarted = false;

    // Fixed configuration values for minimalist approach
    private const int POT_AMOUNT = 1;
    private const float SPAWN_DELAY = 2f;

    public static GameObject? CookingPotPrefab;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        Definition = ModDefinition.GetOrCreate(Info.Metadata);

        string AssetBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "peak_cooking");
        Bundle = AssetBundle.LoadFromFile(AssetBundlePath);

        CookingPotPrefab = Bundle.LoadAsset<GameObject>("CookingPot.prefab");
        // attach behavior
        CookingPotPrefab.AddComponent<CookingPot>();
        new ItemContent(CookingPotPrefab.GetComponent<Item>()).Register(Definition);
        var action = CookingPotPrefab.AddComponent<Action_CookingPotConsume>();
        action.OnCastFinished = true;

        _placeAtSpawn = Config.Bind("General", "SpawnCookingPot", true, "Enable or disable automatic cooking pot placed at spawn");

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Definition.Id);

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private void Start()
    {
        // Subscribe to scene change events to reset state
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Start the main monitoring coroutine
        StartCoroutine(MonitorGameState());
    }

    /// <summary>
    /// Resets mod state when a new scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _gameStarted = false;
        Logger.LogInfo($"Scene changed to: {scene.name} - mod state reset");
    }

    /// <summary>
    /// Main monitoring loop that handles game state and player tracking
    /// </summary>
    private IEnumerator MonitorGameState()
    {
        while (true)
        {
            if (!(_placeAtSpawn?.Value ?? false))
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Only activate in actual game levels, not in Airport (lobby) or other scenes
            if (!IsInValidGameLevel())
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Check if we're connected to Photon and game has started
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                if (!_gameStarted)
                {
                    _gameStarted = true;
                    Logger.LogInfo($"Game started in level: {SceneManager.GetActiveScene().name} - spawning cooking pot");
                    yield return new WaitForSeconds(SPAWN_DELAY);
                    SpawnCookingPots();
                }
            }
            else
            {
                // Reset game state if disconnected
                if (_gameStarted)
                {
                    _gameStarted = false;
                    Logger.LogInfo("Game session ended - resetting cooking pot tracking");
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Validates if we're currently in a game level where bags should be spawned.
    /// Excludes lobby (Airport) and menu scenes, only allows Level_0 through Level_13.
    /// </summary>
    private bool IsInValidGameLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // Check if it's a Level_X scene (Level_0 through Level_13)
        if (sceneName.StartsWith("Level_"))
        {
            string levelNumberStr = sceneName.Substring(6); // Remove "Level_" prefix
            if (int.TryParse(levelNumberStr, out int levelNumber))
            {
                return levelNumber >= 0 && levelNumber <= 13;
            }
        }

        return false;
    }

    /// <summary>
    /// Spawns bags for all players in the current game session.
    /// Only executed by the master client to prevent duplication.
    /// </summary>
    private void SpawnCookingPots()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Logger.LogInfo("Automatically spawning cooking pot for host...");

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsMasterClient)
            {
                StartCoroutine(SpawnCookingPotForPlayerCoroutine(player));
            }
        }
    }

    /// <summary>
    /// Spawns a pot for a specific player
    /// </summary>
    private IEnumerator SpawnCookingPotForPlayerCoroutine(Photon.Realtime.Player player)
    {
        SpawnCookingPotForPlayer(player);
        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// Spawns a single pot for the specified player.
    /// Spawns near player.
    /// </summary>
    private void SpawnCookingPotForPlayer(Photon.Realtime.Player player)
    {
        try
        {
            // Get player's character and Player component
            Character? playerCharacter = GetPlayerCharacter(player);
            if (playerCharacter == null) return;

            Player? playerComponent = GetPlayerComponent(player);
            if (playerComponent == null) return;

            // spawn a pot near the player
            SpawnCookingPotNearPlayer(playerCharacter);
            Logger.LogInfo($"Cooking Pot spawned near player: {player.NickName}");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"Error spawning pot for {player.NickName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds the Character component for a specific Photon player
    /// </summary>
    private Character? GetPlayerCharacter(Photon.Realtime.Player player)
    {
        foreach (var character in Character.AllCharacters)
        {
            if (character != null && character.photonView.Owner == player)
            {
                return character;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the Player component for a specific Photon player
    /// </summary>
    private Player? GetPlayerComponent(Photon.Realtime.Player photonPlayer)
    {
        var playerObjects = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var player in playerObjects)
        {
            if (player.photonView.Owner == photonPlayer)
            {
                return player;
            }
        }
        return null;
    }

    /// <summary>
    /// Spawns a cooking pot item near the specified character
    /// </summary>
    private void SpawnCookingPotNearPlayer(Character character)
    {
        try
        {
            // Calculate spawn position near player
            Vector3 spawnPosition = character.Center + Vector3.up * 1f + Random.insideUnitSphere * 1.5f;
            spawnPosition.y = Mathf.Max(spawnPosition.y, character.Center.y); // Prevent underground spawning

            if (CookingPotPrefab == null) return;

            // Spawn using PhotonNetwork for multiplayer synchronization
            PhotonNetwork.InstantiateItemRoom(CookingPotPrefab.name, spawnPosition, Quaternion.identity);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"Error spawning pot near player: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleanup when plugin is destroyed
    /// </summary>
    private void OnDestroy()
    {
        // Unsubscribe from scene change events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
