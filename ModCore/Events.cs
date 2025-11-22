using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ID2.ModCore;

/// <summary>
/// A collection of common events.
/// </summary>
public static class Events
{
	// Entity
	public static event OnPlayerSpawnFunc OnPlayerSpawned;
	public static event OnEntityKilledFunc OnEntityKilled;

	// Scene
	public static event OnSceneLoadedFunc OnSceneLoaded;
	public static event OnRoomChangedFunc OnRoomChanged;

	// Game
	public static event OnPausedFunc OnPaused;
	public static event Action OnGameQuit;

	// Delegates
	public delegate void OnPlayerSpawnFunc(Entity player);
	public delegate void OnEntityKilledFunc(Entity entity, Killable.DetailedDeathData data);
	public delegate void OnSceneLoadedFunc(Scene scene, LoadSceneMode mode);
	public delegate void OnRoomChangedFunc(LevelRoom from, LevelRoom to, EntityEventsOwner.RoomEventData data);
	public delegate void OnPausedFunc(bool paused);

	internal static void PlayerSpawned(Entity player, GameObject camera, PlayerController controller)
	{
		Globals.UpdateCurrentRoom(null);
		OnPlayerSpawned?.Invoke(player);
	}

	internal static void EntityKilled(Entity entity, Killable.DetailedDeathData data)
	{
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