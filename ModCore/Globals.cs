using UnityEngine;

namespace ID2.ModCore;

/// <summary>
/// A collection of global variables.
/// </summary>
public static class Globals
{
	private static Entity m_player;
	private static SaverOwner m_mainSaver;
	private static string m_spawnPoint;
	private static Font m_vanillaFont;

	/// <summary>
	/// Returns PlayerEnt's <see cref="Entity"/> component.
	/// </summary>
	public static Entity Player
	{
		get
		{
			if (m_player == null)
				m_player = EntityTag.GetEntityByName("PlayerEnt");

			return m_player;
		}
	}

	/// <summary>
	/// Returns the main <see cref="SaverOwner"/>, representing the main save data.
	/// </summary>
	public static SaverOwner MainSaver
	{
		get
		{
			if (m_mainSaver == null)
				m_mainSaver = Resources.Load<GameStatData>("gamestats/Data").Saver;

			return m_mainSaver;
		}
	}

	/// <summary>
	/// Returns the current active scene name.
	/// </summary>
	public static string CurrentScene => Utility.GetCurrentSceneName();

	/// <summary>
	/// Returns the current active <see cref="LevelRoom"/>.
	/// </summary>
	public static LevelRoom CurrentRoom { get; private set; }

	/// <summary>
	/// Returns the name of the spawn point saved to the save file.
	/// </summary>
	public static string SpawnPoint
	{
		get
		{
			if (string.IsNullOrEmpty(m_spawnPoint))
				m_spawnPoint = MainSaver.LocalStorage.GetLocalSaver("start").LoadData("door");

			return m_spawnPoint;
		}
		internal set { m_spawnPoint = value; }
	}

	/// <summary>
	/// Returns <b>true</b> if the game is paused.
	/// </summary>
	public static bool IsPaused { get; internal set; }

	/// <summary>
	/// Returns the vanilla "Cutscene" font used by the game. This is the only font used.
	/// </summary>
	public static Font VanillaFont
	{
		get
		{
			m_vanillaFont ??= Resources.Load<FontMaterialMap>("FontMaterialMap")._data[0].font;
			return m_vanillaFont;
		}
	}

	internal static void UpdateCurrentRoom(LevelRoom currentRoom)
	{
		// If entered from a scene transition, find the only active room
		currentRoom ??= LevelRoom.currentRooms.Find(r => r.IsActive && !r.IsDummy);

		if (currentRoom != null)
			CurrentRoom = currentRoom;
	}
}