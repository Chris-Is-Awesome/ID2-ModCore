using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ID2.ModCore;

[BepInPlugin(PluginInfo.guid, PluginInfo.name, PluginInfo.version)]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	private static Plugin instance;

	public static Plugin Instance => instance;

	private void Awake()
	{
		instance = this;
		Logger = base.Logger;

		try
		{
			// Mod initialization code here
			LoadEmbeddedAssemblies();

			Harmony harmony = new(PluginInfo.guid);
			harmony.PatchAll();
		}
		catch (Exception ex)
		{
			ModCore.Logger.LogError($"Unhandled exception during initialization: {ex.Message}");
			return;
		}

		Logger.LogInfo($"Initialized [{PluginInfo.name} {PluginInfo.version}]");
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
		BackupLogFileOnQuit();
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

	private void BackupLogFileOnQuit()
	{
		string logPath = Helpers.CombinePaths(Paths.BepInExRootPath, "LogOutput.log");

		if (!File.Exists(logPath))
		{
			return;
		}

		string backupDir = Helpers.CombinePaths(Paths.BepInExRootPath, "backup logs");
		DateTime now = DateTime.Now;
		Directory.CreateDirectory(backupDir);

		// Delete old log files
		foreach (string file in Directory.GetFiles(backupDir, "*.log"))
		{
			try
			{
				FileInfo info = new(file);

				if ((now - info.CreationTime).TotalHours > 24)
				{
					info.Delete();
				}
			}
			catch (Exception ex)
			{
				ModCore.Logger.LogWarning($"Failed to delete old log '{file}': {ex.Message}");
			}
		}

		// Copy log file
		string timestamp = now.ToString("yyyy-MM-dd_HH-mm-ss");
		string backupPath = Helpers.CombinePaths(backupDir, $"Log_{timestamp}.log");

		try
		{
			File.Copy(logPath, backupPath, true);
		}
		catch (Exception ex)
		{
			ModCore.Logger.LogError($"Failed to copy log: {ex.Message}");
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