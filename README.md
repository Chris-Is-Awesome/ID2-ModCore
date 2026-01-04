# ModCore

A core dependency for many Ittle Dew 2 mods. Provides many useful helper methods, cached object references, an event API, and more.

## How to Install

1. Install [BepInEx 5.4.22 or later 5.x versions](https://github.com/bepinex/bepinex/releases/latest)
    - Do **not** use BepInEx 6.x, as it may not be compatible.
    - Unzip the BepInEx folder into the root of your game's installation directory (i.e. there should be a `BepInEx` folder in the same folder as `ID2.exe`).
1. Run the game once to generate the BepInEx plugins folder, then quit before performing the next step.
1. Download `ModCore.zip` included in the [latest release](https://github.com/Chris-Is-Awesome/ID2-ModCore/releases/latest). Extract the downloaded zip to your BepInEx plugins folder (`BepInEx\plugins`).

## Features for Modders

<details>
<summary>Events API</summary>

These are accessed via the `Events` static class.

Event | Parameters | Notes
----- | ---- | -----
`OnPlayerSpawned` | `Entity` player | Runs *before* `OnSceneLoaded`.
`OnPlayerKilled` | `Killable.DetailedDeathData` data | Does not run if player is killed by warping from pause menu.
`OnEntityKilled` | `Entity` entity, `Killable.DetailedDeathData` data | Does not run if the Entity killed is `PlayerEnt`.
`OnSceneLoaded` | `Scene` scene, `LoadSceneMode` mode | Runs *after* `OnPlayerSpawned`.
`OnRoomChanged` | `LevelRoom` from, `LevelRoom` to, `EntityEventsOwner.RoomEventData` data | Does not run on scene load—use `OnSceneLoaded` instead.
`OnFileStarted` | `bool` isNewFile, Action onPreloadDone = null | Runs in main menu when a file is loaded or a new file is created. This runs before preloading has finished, and invokes `onPreloadDone` once preloading has completed.
`OnPaused` | `bool` paused
`OnGameQuit` | (none)

</details>

<details>
<summary>Global Cached Objects Reference</summary>

These are accessed via the `Globals` static class.

Object | Info
------ | ----
`Player` | The `Entity` component on the `PlayerEnt` GameObject.
`MainSaver` | The `SaverOwner` for the current save file.
`CurrentScene` | The name of the currently loaded Scene.
`CurrentRoom` | The `LevelRoom` that is currently active.
`SpawnPoint` | The name of the spawn point that's in the save file.
`IsPaused` | True if the game is paused, false otherwise.

</details>

<details>
<summary>Helper Methods</summary>

These are accessed via the `Helpers` static class.

```cs
// Loads the scene by name.
public static void LoadScene(string sceneName, bool additively = false)
```

```cs
// Loads the scene by build index.
public static void LoadScene(int sceneBuildIndex, bool additively = false)
```

```cs
// Creates a Configuration Manager option.
public static ConfigEntry<T> CreateOption<T>(
	ConfigFile configFile,
	string key,
	string description,
	T defaultValue,
	string section = "",
	Action<T> onChanged = null,
	bool neverSave = false,
	ConfigurationManagerAttributes attributes = null,
	AcceptableValueBase acceptableValues = null)
```

```cs
// Creates a KeyboardShortcut Configuration Manager option.
public static ConfigEntry<KeyboardShortcut> CreateOption(
	ConfigFile configFile,
	string key,
	string description,
	KeyboardShortcut defaultValue,
	Action onHotkeyPressed,
	string section = "",
	bool neverSave = false,
	ConfigurationManagerAttributes attributes = null,
	AcceptableValueBase acceptableValues = null)
```

```cs
// Creates a custom Configuration Manager menu.
public static ConfigEntry<bool> CreateMenu(
	ConfigFile configFile,
	Action<ConfigEntryBase> drawer,
	string section = "",
	string key = "",
	string description = "")
```

```cs
// Creates a FadeEffectData object.
public static FadeEffectData MakeFadeData(
	Color targetColor,
	float fadeOutTime = 0.5f,
	float fadeInTime = 1.25f,
	FadeType fadeType = FadeType.ScreenCircleWipe,
	bool useScreenPos = true)
```

```cs
// Converts a hex color string to a Color.
public static Color ConvertHexToColor(string hex)
```

```cs
// Converts a Color to a hex color string.
public static string ConvertColorToHex(Color color)
```

```cs
// Registers a hotkey.
public static void RegisterHotkey(KeyCode key, Action callback)
```

```cs
// Unregisters a hotkey.
public static void UnregisterHotkey(KeyCode key, Action callback)
```

```cs
// Loads an embedded resource.
public static T LoadEmbeddedResource<T>(string resourcePath)
```

```cs
// Deserializes a string of JSOn into an object of type T.
public static T DeserializeJsonObject<T>(string json)
```

```cs
// Creates a custom Canvas GameObject for making custom in-game UI.
public static Canvas CreateCanvas(string objName, RenderMode renderMode = RenderMode.ScreenSpaceOverlay)
```

```cs
// Combines multiple paths into one, automatically using the correct OS-based path separator.
public static string CombinePaths(params string[] paths)
```

```cs
// Creates a Texture2D from an array of bytes.
public static Texture2D CreateTextureFromBytes(byte[] bytes)
```

</details>

<details>
<summary>GUI Helper Methods</summary>

These are helper methods that make it easier to create custom Configuration Manager menus.
These are accessed via the `GUIHelpers` static class.

```cs
// Creates a label that is centered within its container.
public static void CreateCenteredLabel(object text)
```

```cs
// Creates a horizontal layout.
public static void CreateHorizontalLayout(Action callback, params GUILayoutOption[] options)
```

```cs
// Creates a vertical layout.
public static void CreateVerticalLayout(Action callback, params GUILayoutOption[] options)
```

```cs
// Creates a vertical scroll list.
public static void CreateVerticalScrollList(
	object label,
	ref Vector2 currentScrollPos,
	object currentValue,
	string[] values,
	int height,
	out string selectedValue,
	Action<string> onChanged = null
	params GUILayoutOption[] options)
```

```cs
// Creates a horizontal slider with float values.
public static float CreateHorizontalSlider(
	ref float sliderValue,
	float min,
	float max,
	bool showMinMax,
	params GUILayoutOption[] options)
```

```cs
// Creates a horizontal slider with int values.
public static void CreateHorizontalSlider(
	ref int sliderValue,
	int min,
	int max,
	bool showMinMax,
	params GUILayoutOption[] options)
```

```cs
// Creates a collapsible section.
public static void CreateCollapsibleSection(
	ref bool collapsed,
	object text,
	Action callback,
	params GUILayoutOption[] options)
```

```cs
// Creates a button.
public static void CreateButton(object text, Action onClicked)
```

```cs
// Adds color to the layout code used in the callback.
public static void AddColor(string color, Action callback)
```

```cs
// Adds color tags to a string.
public static string ColorText(string text, string color)
```

</details>

<details>
<summary>Preloader</summary>

A system that allows for caching scene objects in an efficient way.

Preloading happens when a file is started or created and loads to the saved scene when done.

Each scene that gets preloaded will invoke the `onLoadedSceneCallback`, allowing mod authors to run any code they want during the preload.

Scenes and `onLoadedSceneCallback`s get dealt with in the order they're sent in. If the given `scene` to preload already exists in preload list, the callback will get appended to the existing scene in the Dictionary.

Public static methods:
```cs
// Returns the preloaded object of the given type and name, if found; null otherwise.
public static T GetPreloadedObject<T>(string objName)
```
```cs
// Adds objects to the preload list. When preloading starts, all objects in this list will get preloaded.
public static void AddObjectToPreloadList(string scene, OnLoadedSceneFunc onLoadedSceneCallback)
```
```cs
public delegate Object[] OnLoadedSceneFunc();
```

Example usage:
```cs
// Caches the MatriarchSpawner from Bad Dream
Events.OnFileStarted += (bool isNewFile, System.Action onPreloadDone = null) => 
{
	Preloader.AddObjectToPreloadList("Deep26", () =>
	{
		return [
			GameObject.Find("MatriarchSpawner")
		];
	});

	onPreloadDone = () =>
	{
		// Do stuff after preload
	}
};
```
```cs
// Fetches the cached MatriarchSpawner
Preloader.GetPreloadedObject<GameObject>("MatriarchSpawner");
```

</details>

## Libraries Used
- [Newtonsoft.Json](https://www.newtonsoft.com/json)