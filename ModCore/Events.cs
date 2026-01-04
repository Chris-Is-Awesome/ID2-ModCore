using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ID2.ModCore;

/// <summary>
/// A collection of common events.
/// </summary>
public static class Events
{
	// Entity
	/// <summary>
	/// Runs when the player has spawned. <br/><br/>
	/// 
	/// Notes: <br/>
	/// - This does <i>NOT</i> run when player respawns (eg. voiding, dying, warping) <br/>
	/// - This runs <i>BEFORE</i> <see cref="OnSceneLoaded"/>
	/// </summary>
	public static event OnPlayerSpawnFunc OnPlayerSpawned;
	/// <summary>
	/// Runs when Ittle dies by any means other than warping.
	/// </summary>
	public static event OnPlayerKilledFunc OnPlayerKilled;
	/// <summary>
	/// Runs when an <see cref="Entity"/> is killed. <br/><br/>
	/// 
	/// Notes: <br/>
	/// - Does <i>NOT</i> run when Ittle dies. Use <see cref="OnPlayerKilled"/> for that.
	/// </summary>
	public static event OnEntityKilledFunc OnEntityKilled;

	// Scene
	/// <summary>
	/// Runs when a scene has finished loading. <br/><br/>
	/// 
	/// Notes: <br/>
	/// - Runs <i>AFTER</i> <see cref="OnPlayerSpawned"/>.
	/// </summary>
	public static event OnSceneLoadedFunc OnSceneLoaded;
	/// <summary>
	/// Runs when the player has moved from one room to another. <br/><br/>
	/// 
	/// Notes: <br/>
	/// - This does <i>NOT</i> run in the initial room load after a scene change (eg. entering a dungeon). It only runs when switching between <b>rooms</b>, <i>not</i> scenes. Use <see cref="OnSceneLoaded"/> to detect the initial room load, as they do the same thing.
	/// </summary>
	public static event OnRoomChangedFunc OnRoomChanged;

	// Game
	/// <summary>
	/// Runs in MainMenu either when an existing file is loaded or when a new file is created.
	/// </summary>
	public static event OnFileStartedFunc OnFileStarted;
	/// <summary>
	/// Runs when the game is paused (pause menu is open).
	/// </summary>
	public static event OnPausedFunc OnPaused;
	/// <summary>
	/// Runs when the game is quit by normal means (does <i>not</i> run for forced closes or crashes). Useful for cleanup.
	/// </summary>
	public static event Action OnGameQuit;

	// Delegates
	public delegate void OnPlayerSpawnFunc(Entity player);
	public delegate void OnEntityKilledFunc(Entity entity, Killable.DetailedDeathData data);
	public delegate void OnPlayerKilledFunc(Killable.DetailedDeathData data);
	public delegate void OnSceneLoadedFunc(Scene scene, LoadSceneMode mode);
	public delegate void OnRoomChangedFunc(LevelRoom from, LevelRoom to, EntityEventsOwner.RoomEventData data);
	public delegate void OnFileStartedFunc(bool isNewFile, Action onPreloadDone = null);
	public delegate void OnPausedFunc(bool paused);

	internal static void PlayerSpawned(Entity player, GameObject camera, PlayerController controller)
	{
		Globals.UpdateCurrentRoom(null);
		OnPlayerSpawned?.Invoke(player);
	}

	internal static void EntityKilled(Entity entity, Killable.DetailedDeathData data)
	{
		// Prevents calling player killed event if player voids out without dying
		if (data.hp > 0)
		{
			return;
		}

		if (entity == Globals.Player)
		{
			StackTrace stack = new StackTrace(5, false);
			MethodBase method = stack.GetFrame(0).GetMethod();

			// Don't run if player died by warping
			if (method.DeclaringType.Name.Contains("ForceRespawn"))
			{
				return;
			}

			OnPlayerKilled?.Invoke(data);
			return;
		}

		OnEntityKilled?.Invoke(entity, data);
	}

	internal static void SceneLoaded(Scene scene, LoadSceneMode mode)
	{
		PlayerSpawner.RegisterSpawnListener(PlayerSpawned);
		OnSceneLoaded?.Invoke(scene, mode);
	}

	internal static void RoomChangeed(LevelRoom from, LevelRoom to, EntityEventsOwner.RoomEventData data)
	{
		Globals.UpdateCurrentRoom(to);
		OnRoomChanged?.Invoke(from, to, data);
	}

	internal static void FileStarted(bool isNewFile, Action onPreloadDone = null)
	{
		OnFileStarted?.Invoke(isNewFile);

		Preloader.Instance.StartPreload(() =>
		{
			onPreloadDone?.Invoke();
		});
	}

	internal static void Paused(bool pause)
	{
		Globals.IsPaused = pause;
		OnPaused?.Invoke(pause);
	}

	internal static void GameQuit()
	{
		OnGameQuit?.Invoke();
	}
}