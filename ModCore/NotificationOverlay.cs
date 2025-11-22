using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ID2.ModCore;

internal class NotificationOverlay : MonoBehaviour
{
	private readonly Vector2 position = new Vector2(25, -1025);
	private readonly List<Outline> textOutlines = new();
	private readonly string colorHex = "#ffffff00";
	private readonly string outlineColorHex = "#00000000";
	private readonly int fontSize = 18;
	private readonly int outlineThickness = 2;
	private readonly float showTime = 5;
	private readonly float fadeDuration = 0.5f;
	private Font font;
	private GameObject textObj;
	private Text text;
	private Coroutine fadeRoutine;
	private float showTimer;

	private void Awake()
	{
		font = Resources.Load<FontMaterialMap>("FontMaterialMap")._data[0].font;

		textObj = new GameObject("NotificationText");
		textObj.transform.SetParent(transform, false);

		text = textObj.AddComponent<Text>();
		text.font = font;
		text.fontSize = fontSize;
		text.alignment = TextAnchor.LowerLeft;
		text.color = Helpers.ConvertHexToColor(colorHex);

		RectTransform textRect = text.rectTransform;
		textRect.pivot = new Vector2(0, 1);
		textRect.anchorMin = textRect.pivot;
		textRect.anchorMax = textRect.pivot;
		textRect.anchoredPosition = position;

		for (int i = 0; i < outlineThickness; i++)
		{
			Outline textOutline = textObj.AddComponent<Outline>();
			textOutline.effectColor = Helpers.ConvertHexToColor(outlineColorHex);
			textOutline.effectDistance = new Vector2(1f, -1f);
			textOutlines.Add(textOutline);
		}

		ContentSizeFitter fitter = textObj.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		if (!textObj.activeSelf)
			return;

		showTimer += Time.deltaTime;

		if (showTimer >= showTime)
		{
			if (fadeRoutine != null)
				StopCoroutine(fadeRoutine);

			fadeRoutine = StartCoroutine(FadeText(0));
			showTimer = 0;
		}
	}

	public void ShowText(string text)
	{
		Color textColor = this.text.color;
		textColor.a = 0;
		this.text.color = textColor;
		this.text.text = text;

		if (fadeRoutine != null)
			StopCoroutine(fadeRoutine);

		fadeRoutine = StartCoroutine(FadeText(1));
	}

	private void UpdateTextAlpha(float alpha)
	{
		Color newColor = text.color;
		newColor.a = alpha;
		text.color = newColor;

		for (int i = 0; i < textOutlines.Count; i++)
		{
			newColor = textOutlines[i].effectColor;
			newColor.a = alpha;
			textOutlines[i].effectColor = newColor;
		}
	}

	private IEnumerator FadeText(float targetAlpha)
	{
		float newAlpha;
		float startAlpha = text.color.a;
		float t = 0;

		while (t < fadeDuration)
		{
			t += Time.deltaTime;
			newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
			UpdateTextAlpha(newAlpha);
			yield return null;
		}

		newAlpha = targetAlpha;
		UpdateTextAlpha(newAlpha);
	}
}