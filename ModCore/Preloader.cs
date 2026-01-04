using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ID2.ModCore;

public class Preloader
{
	private static Preloader instance;
	private readonly FadeEffectData fadeData;
	private static Dictionary<string, List<OnLoadedSceneFunc>> objectsToPreload = [];
	private static List<Object> preloadedObjects = [];
	private static GameObject preloadingScreen;
	private Transform objectHolder;
	private Stopwatch stopwatch;

	public static bool IsPreloading { get; private set; }
	internal static Preloader Instance
	{
		get
		{
			instance ??= new Preloader();
			return instance;
		}
	}

	private Preloader()
	{
		instance = this;

		// Create fade data for transitioning to/from preload
		fadeData = Helpers.MakeFadeData(
			Color.black,
			fadeOutTime: 0.5f,
			fadeInTime: 1.25f,
			fadeType: FadeType.ScreenCircleWipe,
			useScreenPos: true
		);

		Events.OnSceneLoaded += OnSceneLoaded;
	}

	/// <summary>
	/// Returns the preloaded object of the given type and name, if found; null otherwise.
	/// </summary>
	/// <typeparam name="T">The type of the object.</typeparam>
	/// <param name="objName">The name of the object.</param>
	public static T GetPreloadedObject<T>(string objName) where T : Object
	{
		// Find the preloaded object
		T obj = (T)preloadedObjects.Find(x => x.name == objName);

		if (obj == null)
		{
			Logger.LogError($"No object with name '{objName}' was found in preload list. Has it been preloaded?");
			return null;
		}

		return obj;
	}

	/// <summary>
	/// Adds objects to the preload list. When preloading starts, all objects in this list will get preloaded.
	/// </summary>
	/// <param name="scene">The name of the scene the objects you want to preload are in.</param>
	/// <param name="onLoadedSceneCallback">The callback to run once the scene has loaded during preload. The objects returned in this delegate are what get preloaded.</param>
	public static void AddObjectToPreloadList(string scene, OnLoadedSceneFunc onLoadedSceneCallback)
	{
		// If the scene is already in preload list, add the new callback to it
		if (objectsToPreload.ContainsKey(scene))
		{
			if (objectsToPreload[scene].Contains(onLoadedSceneCallback))
			{
				return;
			}

			objectsToPreload[scene].Add(onLoadedSceneCallback);
		}
		// If the scene is not already in preload list, add the new scene
		else
		{
			objectsToPreload.Add(scene, [onLoadedSceneCallback]);
		}
	}

	internal void StartPreload(System.Action onDone = null)
	{
		objectHolder = new GameObject("Preloaded Objects").transform;
		objectHolder.gameObject.SetActive(false);
		Object.DontDestroyOnLoad(objectHolder);

		IsPreloading = true;
		Logger.Log($"Starting preload of {objectsToPreload.Count} scene(s)...");
		Plugin.StartRoutine(PreloadAll(onDone));
	}

	private IEnumerator PreloadAll(System.Action onDone)
	{
		PreloadingScreen preloadingScreen = new();
		bool hasDoneFadeOut = false;
		int loopCount = 0;

		foreach (var kvp in objectsToPreload)
		{
			loopCount++;
			string sceneToLoad = kvp.Key;

			// Does fadeout after clicking start game
			if (!hasDoneFadeOut)
			{
				OverlayFader.StartFade(fadeData, true, delegate ()
				{
					// Fadeout done
					stopwatch = Stopwatch.StartNew();
					preloadingScreen.TogglePreloadingScreen();
					hasDoneFadeOut = true;
				}, Vector3.zero);
			}

			// Wait for fadeout to finish
			yield return new WaitUntil(() => { return hasDoneFadeOut; });

			// Wait for scene to load
			yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
			Logger.Log($"Preloading scene {sceneToLoad}...");
			Scene sceneToUnload = SceneManager.GetActiveScene();
			SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
			yield return SceneManager.UnloadSceneAsync(sceneToUnload);

			PreloadObjects(kvp.Value);
			preloadingScreen.UpdateProgress(loopCount, objectsToPreload.Count);
		}

		preloadingScreen.TogglePreloadingScreen();
		PreloadingDone(onDone);
	}

	private void PreloadObjects(List<OnLoadedSceneFunc> callbacks)
	{
		for (int i = 0; i < callbacks.Count; i++)
		{
			// Get the objects to preload from the callback
			Object[] objs = callbacks[i]?.Invoke();

			foreach (Object obj in objs)
			{
				GameObject gameObj = obj as GameObject;

				// If Object is a GameObject, change its parent so it persists
				if (gameObj != null)
					gameObj.transform.SetParent(objectHolder, true);
				else
					Object.DontDestroyOnLoad(obj);

				preloadedObjects.Add(obj);
			}
		}
	}

	private void PreloadingDone(System.Action onDone)
	{
		IsPreloading = false;
		objectsToPreload.Clear();
		Object.Destroy(preloadingScreen);

		stopwatch.Stop();
		Logger.Log($"Finished preloading {preloadedObjects.Count} object(s) across {objectsToPreload.Count} scene(s) in {stopwatch.ElapsedMilliseconds}ms");

		onDone?.Invoke();

		// Load into saved scene
		IDataSaver startSaver = Globals.MainSaver.GetSaver("/local/start");
		string savedScene = startSaver.LoadData("level");
		string sceneToLoad = string.IsNullOrEmpty(savedScene) ? "Intro" : savedScene;
		fadeData._fadeOutTime = 0;
		SceneDoor.StartLoad(sceneToLoad, startSaver.LoadData("door"), fadeData);
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == "MainMenu")
		{
			preloadedObjects.Clear();
			Object.Destroy(objectHolder.gameObject);
		}
	}

	public delegate Object[] OnLoadedSceneFunc();

	private class PreloadingScreen
	{
		private Text progressText;

		public void TogglePreloadingScreen()
		{
			if (preloadingScreen == null)
			{
				CreateUI();
			}

			preloadingScreen.SetActive(!preloadingScreen.activeSelf);
		}

		public void UpdateProgress(int current, int total)
		{
			if (preloadingScreen == null)
			{
				return;
			}

			int percent = (int)(current / (float)total * 100);
			progressText.text = $"Preloading in progress...\n{percent}% ({current}/{total})";
		}

		private void CreateUI()
		{
			preloadingScreen = new GameObject("Preloading Screen");
			preloadingScreen.SetActive(false);

			CreateCanvas();
			CreateBackground();
			CreateText();

			Object.DontDestroyOnLoad(preloadingScreen);
		}

		private void CreateCanvas()
		{
			Canvas canvas = preloadingScreen.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			preloadingScreen.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			preloadingScreen.AddComponent<GraphicRaycaster>();
		}

		private void CreateBackground()
		{
			GameObject background = new GameObject("Background", typeof(Image));
			background.transform.SetParent(preloadingScreen.transform);

			Image image = background.GetComponent<Image>();
			image.color = Color.black;

			RectTransform rect = image.rectTransform;
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}

		private void CreateText()
		{
			GameObject textObj = new GameObject("Text", typeof(Text));
			textObj.transform.SetParent(preloadingScreen.transform, false);

			progressText = textObj.GetComponent<Text>();
			progressText.text = "Preloading in progress...\n0% (0/0)";
			progressText.font = Globals.VanillaFont;
			progressText.fontSize = 24;
			progressText.color = Color.white;
			progressText.alignment = TextAnchor.MiddleCenter;

			RectTransform rect = progressText.rectTransform;
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(400, 50);
		}
	}

	[HarmonyPatch]
	private class Patches
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ChangeRespawnerEventObserver), nameof(ChangeRespawnerEventObserver.DoChange))]
		private static bool PreventRemedyCheckpointFromSavingPatch()
		{
			return !IsPreloading;
		}
	}
}