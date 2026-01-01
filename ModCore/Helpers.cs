using BepInEx.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ID2.ModCore;

/// <summary>
/// A collection of helper methods.
/// </summary>
public static class Helpers
{
	private static readonly Dictionary<ConfigFile, int> optionOrders = new();
	private static readonly List<HotkeyData> hotkeyListeners = new();
	private static NotificationOverlay notifOverlay;

	internal static void Update()
	{
		for (int i = 0; i < hotkeyListeners.Count; i++)
		{
			HotkeyData data = hotkeyListeners[i];

			if (Input.GetKeyDown(data.Hotkey))
				InvokeHotkey(data);
		}
	}

	private static void InvokeHotkey(HotkeyData data)
	{
		data.Callback?.Invoke();

		notifOverlay ??= CreateCanvas("Hotkey Overlay").gameObject.AddComponent<NotificationOverlay>();
		notifOverlay.ShowText($"Hotkey {data.Hotkey} pressed");
	}

	/// <summary>
	/// Loads the scene by name.
	/// </summary>
	/// <param name="sceneName">The name of the scene.</param>
	/// <param name="additively">If <b>true</b>, the scene will be loaded additively.</param>
	public static void LoadScene(string sceneName, bool additively = false)
	{
		SceneManager.LoadScene(sceneName, additively ? LoadSceneMode.Additive : LoadSceneMode.Single);
	}

	/// <summary>
	/// Loads the scene by build index.
	/// </summary>
	/// <param name="sceneBuildIndex">The build index for the scene.</param>
	/// <param name="additively">If <b>true</b>, the scene will be loaded additively.</param>
	public static void LoadScene(int sceneBuildIndex, bool additively = false)
	{
		SceneManager.LoadScene(sceneBuildIndex, additively ? LoadSceneMode.Additive : LoadSceneMode.Single);
	}

	/// <summary>
	/// Creates a <see cref="ConfigEntry{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the <see cref="ConfigEntry{T}"/>.</typeparam>
	/// <param name="configFile">The Plugin's <see cref="ConfigFile"/>.</param>
	/// <param name="key">The label text.</param>
	/// <param name="description">The hover text.</param>
	/// <param name="defaultValue">The default value. Must be of type <typeparamref name="T"/>.</param>
	/// <param name="section">The section header text.</param>
	/// <param name="onChanged">The callback to invoke when the value of this <see cref="ConfigEntry{T}"/> changes.</param>
	/// <param name="neverSave">If <b>true</b>, the value will always reset to its default value; it never saves.<br/>If <b>false</b>, it uses the saved value.</param>
	/// <param name="attributes">The <see cref="ConfigurationManagerAttributes"/> used to change how this <see cref="ConfigEntry{T}"/> gets drawn.</param>
	/// <param name="acceptableValues">The <see cref="AcceptableValueBase"/> used to restrict the values for this <see cref="ConfigEntry{T}"/></param>
	/// <returns>The created <see cref="ConfigEntry{T}"/>.</returns>
	public static ConfigEntry<T> CreateOption<T>(ConfigFile configFile, string key, string description, T defaultValue, string section = "", Action<T> onChanged = null, bool neverSave = false, ConfigurationManagerAttributes attributes = null, AcceptableValueBase acceptableValues = null)
	{
		if (!optionOrders.TryGetValue(configFile, out int order))
			order = int.MaxValue;

		order--;
		optionOrders[configFile] = order;

		ConfigurationManagerAttributes attr = attributes ?? new();
		attr.Order = order;
		ConfigDescription desc = new ConfigDescription(description, acceptableValues, attr);
		ConfigEntry<T> entry = configFile.Bind(section, key, defaultValue, desc);

		if (neverSave)
			entry.Value = defaultValue;

		if (onChanged != null)
			entry.SettingChanged += (s, e) => onChanged(entry.Value);

		return entry;
	}

	/// <summary>
	/// Creates a <see cref="ConfigEntry{T}"/>.
	/// </summary>
	/// <param name="configFile">The Plugin's <see cref="ConfigFile"/>.</param>
	/// <param name="key">The label text.</param>
	/// <param name="description">The hover text.</param>
	/// <param name="defaultValue">The default value. Must be of type <see cref="KeyboardShortcut"/>.</param>
	/// <param name="onHotkeyPressed">The callback to run when the hotkey is pressed.</param>
	/// <param name="section">The section header text.</param>
	/// <param name="neverSave">If <b>true</b>, the value will always reset to its default value; it never saves.<br/>If <b>false</b>, it uses the saved value.</param>
	/// <param name="attributes">The <see cref="ConfigurationManagerAttributes"/> used to change how this <see cref="ConfigEntry{T}"/> gets drawn.</param>
	/// <param name="acceptableValues">The <see cref="AcceptableValueBase"/> used to restrict the values for this <see cref="ConfigEntry{T}"/></param>
	/// <returns>The created <see cref="ConfigEntry{T}"/>.</returns>
	public static ConfigEntry<KeyboardShortcut> CreateOption(ConfigFile configFile, string key, string description, KeyboardShortcut defaultValue, Action onHotkeyPressed, string section = "", bool neverSave = false, ConfigurationManagerAttributes attributes = null, AcceptableValueBase acceptableValues = null)
	{
		ConfigEntry<KeyboardShortcut> entry = CreateOption(
			configFile: configFile,
			key: key,
			description: description,
			defaultValue: defaultValue,
			section: section,
			onChanged: value =>
			{
				HotkeyData hotkey = GetHotkeyFromCallback(onHotkeyPressed);

				if (hotkey != null)
					UnregisterHotkey(hotkey.Hotkey, onHotkeyPressed);

				RegisterHotkey(value.MainKey, onHotkeyPressed);
			},
			neverSave: neverSave,
			attributes: attributes,
			acceptableValues: acceptableValues);
		RegisterHotkey(entry.Value.MainKey, onHotkeyPressed);

		return entry;
	}

	/// <summary>
	/// Creates a custom drawer to allow for more control of the layout.
	/// </summary>
	/// <param name="configFile">The Plugin's <see cref="ConfigFile"/>.</param>
	/// <param name="drawer">The custom drawer method.</param>
	/// <param name="section">The section header text.</param>
	/// <param name="key">The label text.</param>
	/// <param name="description">The description.</param>
	/// <returns>A dummy <see cref="ConfigEntry{T}"/>.</returns>
	public static ConfigEntry<bool> CreateMenu(ConfigFile configFile, Action<ConfigEntryBase> drawer, string section = "", string key = "", string description = "")
	{
		return CreateOption
		(
			configFile: configFile,
			section: section,
			key: key,
			description: description,
			defaultValue: true,
			attributes: new ConfigurationManagerAttributes() { CustomDrawer = drawer, HideSettingName = true, HideDefaultButton = true }
		);
	}

	/// <summary>
	/// Creates a new <see cref="FadeEffectData"/>.
	/// </summary>
	/// <param name="targetColor">The color the fade should use.</param>
	/// <param name="fadeOutTime">The time in seconds for the fade out duration.</param>
	/// <param name="fadeInTime">The time in seconds for the fade in duration.</param>
	/// <param name="fadeType">The type of fade animation.</param>
	/// <param name="useScreenPos">If <b>true</b>, the fade will start at the center of the screen.</param>
	/// <returns>The created <see cref="FadeEffectData"/>.</returns>
	public static FadeEffectData MakeFadeData(Color targetColor, float fadeOutTime = 0.5f, float fadeInTime = 1.25f, FadeType fadeType = FadeType.ScreenCircleWipe, bool useScreenPos = true)
	{
		FadeEffectData fadeData = ScriptableObject.CreateInstance<FadeEffectData>();
		fadeData._targetColor = targetColor;
		fadeData._fadeOutTime = fadeOutTime;
		fadeData._fadeInTime = fadeInTime;
		fadeData._faderName = fadeType.ToString();
		fadeData._useScreenPos = useScreenPos;
		return fadeData;
	}

	/// <summary>
	/// Converts the hex color string to a <see cref="Color"/>.
	/// </summary>
	/// <param name="hex">The hex color string to convert.</param>
	/// <returns>The resulting <see cref="Color"/>.</returns>
	public static Color ConvertHexToColor(string hex)
	{
		ColorUtility.TryParseHtmlString(hex, out Color color);
		return color;
	}

	/// <summary>
	/// Converts a <see cref="Color"/> to a hex color string.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>The resulting hex color string.</returns>
	public static string ConvertColorToHex(Color color)
	{
		return ColorUtility.ToHtmlStringRGBA(color);
	}

	/// <summary>
	/// Registers a hotkey, so when that hotkey is pressed, it will fire the callback.
	/// </summary>
	/// <param name="key">The <see cref="KeyCode"/> to listen for.</param>
	/// <param name="callback">The callback to invoke when the key is pressed.</param>
	public static void RegisterHotkey(KeyCode key, Action callback)
	{
		if (callback == null || key == KeyCode.None)
			return;

		foreach (HotkeyData data in hotkeyListeners)
		{
			if (data.Hotkey == key)
			{
				Logger.LogWarning($"Duplicate hotkeys detected: {key}");

				if (data.Callback == callback)
				{
					Logger.LogError($"Tried to add duplicate hotkey and callback!");
					return;
				}
			}
		}

		hotkeyListeners.Add(new HotkeyData(key, callback));
	}

	/// <summary>
	/// Unregisters a hotkey, so pressing the hotkey will not do anything.
	/// </summary>
	/// <param name="key">The <see cref="KeyCode"/> to stop listening to.</param>
	/// <param name="callback">The callback to stop invoking when the key is pressed.</param>
	public static void UnregisterHotkey(KeyCode key, Action callback)
	{
		HotkeyData foundData = hotkeyListeners.Find(h => h.Hotkey == key && h.Callback == callback);

		if (foundData != null)
			hotkeyListeners.Remove(foundData);
	}

	/// <summary>
	/// Loads an embedded resource from the given path.
	/// </summary>
	/// <typeparam name="T">The type of the resource.</typeparam>
	/// <param name="resourcePath">The relative path to the resource.</param>
	public static T LoadEmbeddedResource<T>(string resourcePath)
	{
		if (string.IsNullOrEmpty(resourcePath))
			throw new ArgumentException("Resource path cannot be null or empty.", nameof(resourcePath));

		Assembly assembly = Assembly.GetCallingAssembly();
		resourcePath = resourcePath.Replace('/', '.').Replace('\\', '.');

		using Stream stream = assembly.GetManifestResourceStream(resourcePath);

		if (stream == null)
			throw new FileNotFoundException($"Embedded resource not found: {resourcePath}");

		if (typeof(T) == typeof(string))
		{
			using StreamReader reader = new(stream);
			object result = reader.ReadToEnd();
			return (T)result;
		}
		else if (typeof(T) == typeof(byte[]))
		{
			using (MemoryStream ms = new())
			{
				byte[] buffer = new byte[81920]; // 80 KB buffer
				int read;

				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
					ms.Write(buffer, 0, read);

				object result = ms.ToArray();
				return (T)result;
			}
		}
		else
			throw new NotSupportedException($"Type {typeof(T)} is not supported.");
	}

	/// <summary>
	/// Deserializes a string of JSON into an object of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="json">The JSON string.</param>
	public static T DeserializeJsonObject<T>(string json)
	{
		return JsonConvert.DeserializeObject<T>(json);
	}

	/// <summary>
	/// Creates a <see cref="Canvas"/> with some additional components.
	/// </summary>
	/// <param name="objName">The name for the created GameObject.</param>
	/// <param name="renderMode">The <see cref="RenderMode"/> for the <see cref="Canvas"/>.</param>
	/// <returns>The created <see cref="Canvas"/>.</returns>
	public static Canvas CreateCanvas(string objName, RenderMode renderMode = RenderMode.ScreenSpaceOverlay)
	{
		GameObject obj = new(objName);

		Canvas canvas = obj.AddComponent<Canvas>();
		canvas.renderMode = renderMode;
		canvas.sortingOrder = 999;

		CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);

		return canvas;
	}

	/// <summary>
	/// Combines multiple paths into one, automatically using the correct OS-based path separator.
	/// </summary>
	/// <param name="paths">The paths to combine.</param>
	/// <returns>The combined path as a string.</returns>
	public static string CombinePaths(params string[] paths)
	{
		return paths.Aggregate(Path.Combine);
	}

	/// <summary>
	/// Creates a <see cref="Texture2D"/> from an array of bytes.
	/// </summary>
	/// <param name="bytes">The array of bytes.</param>
	public static Texture2D CreateTextureFromBytes(byte[] bytes)
	{
		try
		{
			Texture2D texture = new(512, 512, TextureFormat.RGBA32, false);
			texture.LoadImage(bytes);
			return texture;
		}
		catch (Exception ex)
		{
			throw new Exception($"Error: {ex.Message}");
		}
	}

	private static HotkeyData GetHotkeyFromCallback(Action callback)
	{
		return hotkeyListeners.Find(h => h.Callback == callback);
	}

	private class HotkeyData
	{
		public KeyCode Hotkey { get; private set; }
		public Action Callback { get; private set; }

		public HotkeyData(KeyCode hotkey, Action callback)
		{
			Hotkey = hotkey;
			Callback = callback;
		}
	}
}