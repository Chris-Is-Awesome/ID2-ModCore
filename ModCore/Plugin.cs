using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ID2.ModCore;

[BepInPlugin("id2.ModCore", "ModCore", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	private static Plugin instance;

	public static Plugin Instance => instance;

	private void Awake()
	{
		instance = this;
		Logger = base.Logger;

		Logger.LogInfo($"Plugin ModCore (id2.ModCore) is loaded!");

		try
		{
			// Mod initialization code here
			LoadEmbeddedAssemblies();

			var harmony = new Harmony("id2.ModCore");
			harmony.PatchAll();
		}
		catch (System.Exception err)
		{
			Logger.LogError(err);
		}
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += Events.SceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= Events.SceneLoaded;
	}

	private void Update()
	{
		Helpers.Update();
	}

	private void OnApplicationQuit()
	{
		Events.GameQuit();
	}

	private void LoadEmbeddedAssemblies()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		string[] resourceNames = assembly.GetManifestResourceNames()
			.Where(n => n.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase))
			.ToArray();

		foreach (string name in resourceNames)
		{
			try
			{
				using (Stream stream = assembly.GetManifestResourceStream(name))
				{
					byte[] raw = new byte[stream.Length];
					stream.Read(raw, 0, raw.Length);
					Assembly.Load(raw);
				}
			}
			catch (System.Exception ex)
			{
				Logger.LogError($"Failed to load {name}: {ex}");
			}
		}
	}

	/// <summary>
	/// Starts a Coroutine on the Plugin MonoBehaviour.<br/>
	/// This is useful for if you need to start a Coroutine<br/>from a non-MonoBehaviour class.
	/// </summary>
	/// <param name="routine">The routine to start.</param>
	/// <returns>The started Coroutine.</returns>
	public static Coroutine StartRoutine(IEnumerator routine)
	{
		return Instance.StartCoroutine(routine);
	}
}