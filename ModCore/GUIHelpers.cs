using System;
using UnityEngine;

namespace ID2.ModCore;

public static class GUIHelpers
{
	private static Color defaultColor = GUI.color;

	/// <summary>
	/// Creates a lable that is centered within its container.
	/// </summary>
	/// <param name="text">The text for the label.</param>
	public static void CreateCenteredLabel(object text)
	{
		GUILayout.Label(text.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
	}

	/// <summary>
	/// Creates a horizontal layout.
	/// </summary>
	/// <param name="callback">The code to run within the layout.</param>
	/// <param name="options"><see cref="GUILayoutOption"/> overrides to change how the element looks.</param>
	public static void CreateHorizontalLayout(Action callback, params GUILayoutOption[] options)
	{
		GUILayout.BeginHorizontal(options);
		callback?.Invoke();
		GUILayout.EndHorizontal();
	}

	/// <summary>
	/// Creates a vertical layout.
	/// </summary>
	/// <param name="callback">The code to run within the layout.</param>
	/// <param name="options"><see cref="GUILayoutOption"/> overrides to change how the element looks.</param>
	public static void CreateVerticalLayout(Action callback, params GUILayoutOption[] options)
	{
		GUILayout.BeginVertical(options);
		callback?.Invoke();
		GUILayout.EndVertical();
	}

	/// <summary>
	/// Creates a vertical scroll list.
	/// </summary>
	/// <param name="label">The text for </param>
	/// <param name="currentScrollPos">The current scroll position. You need to cache this.</param>
	/// <param name="currentValue">The current selected value.</param>
	/// <param name="values">An array of values that fills the scroll list.</param>
	/// <param name="height">The height for the element.</param>
	/// <param name="selectedValue">The resulting value that is selected.</param>
	/// <param name="onChanged">Callback to run when the selection has changed.</param>
	/// <param name="options"><see cref="GUILayoutOption"/> overrides to change how the element looks.</param>
	public static void CreateVerticalScrollList(object label, ref Vector2 currentScrollPos, object currentValue, string[] values, int height, out string selectedValue, Action<string> onChanged = null, params GUILayoutOption[] options)
	{
		Vector2 scrollPos = currentScrollPos;
		Vector2 oldScrollPos = scrollPos;
		string newSelectedValue = string.Empty;

		CreateVerticalLayout(() =>
		{
			if (label != null && !string.IsNullOrEmpty(label.ToString()))
				CreateCenteredLabel(label);

			scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(height));
			int index = Array.IndexOf(values, currentValue);
			index = GUILayout.SelectionGrid(index, values, 1);
			newSelectedValue = values[index];
			GUILayout.EndScrollView();
		}, options);

		currentScrollPos = scrollPos;
		selectedValue = newSelectedValue;

		if (onChanged != null && currentValue.ToString() != newSelectedValue)
			onChanged(newSelectedValue);
	}

	/// <summary>
	/// Creates a horizontal slider.
	/// </summary>
	/// <param name="sliderValue">The current value for the slider. You need to cache this.</param>
	/// <param name="min">The minimum value for the slider.</param>
	/// <param name="max">The maximum value for the slider.</param>
	/// <param name="showMinMax">If <b>true</b>, there will be labels to the sides of the slider that show min/max.</param>
	/// <param name="options"><see cref="GUILayoutOption"/> overrides to change how the element looks.</param>
	public static float CreateHorizontalSlider(ref float sliderValue, float min, float max, bool showMinMax, params GUILayoutOption[] options)
	{
		float newSliderValue = sliderValue;

		CreateHorizontalLayout(() =>
		{
			if (showMinMax)
			{
				// Column - Min
				CreateVerticalLayout(() => CreateCenteredLabel(min));

				// Column - Slider
				CreateVerticalLayout(() =>
				{
					// Aligns slider better with the min/max columns
					GUILayout.Space(8);
					newSliderValue = GUILayout.HorizontalSlider(newSliderValue, min, max, options);
				});

				// Column - Max
				CreateVerticalLayout(() => CreateCenteredLabel(max));
			}
			else
			{
				// Column - Slider
				CreateVerticalLayout(() =>
				{
					newSliderValue = GUILayout.HorizontalSlider(0, min, max, options);
				});
			}
		});

		sliderValue = newSliderValue;
		return sliderValue;
	}

	/// <summary>
	/// Creates a horizontal slider.
	/// </summary>
	/// <param name="sliderValue">The current value for the slider. You need to cache this.</param>
	/// <param name="min">The minimum value for the slider.</param>
	/// <param name="max">The maximum value for the slider.</param>
	/// <param name="showMinMax">If <b>true</b>, there will be labels to the sides of the slider that show min/max.</param>
	/// <param name="options"><see cref="GUILayoutOption"/> overrides to change how the element looks.</param>
	public static void CreateHorizontalSlider(ref int sliderValue, int min, int max, bool showMinMax, params GUILayoutOption[] options)
	{
		float newSliderValue = sliderValue;
		newSliderValue = CreateHorizontalSlider(ref newSliderValue, min, max, showMinMax, options);
		sliderValue = Mathf.RoundToInt(newSliderValue);
	}

	/// <summary>
	/// Creates a collapsible section.
	/// </summary>
	/// <param name="collapsed">The current collapsed value. You need to cache this.</param>
	/// <param name="text">The text for the toggle.</param>
	/// <param name="callback">The code to run within the layout.</param>
	/// <param name="options"><see cref="GUILayoutOption"/> overrides to change how the element looks.</param>
	public static void CreateCollapsibleSection(ref bool collapsed, object text, Action callback, params GUILayoutOption[] options)
	{
		collapsed = GUILayout.Toggle(collapsed, $"{(!collapsed ? $"▼ {text}" : $"► {text}")}", "Button", options);

		if (!collapsed)
		{
			GUILayout.BeginVertical();
			callback?.Invoke();
			GUILayout.EndVertical();
		}
	}

	/// <summary>
	/// Creates a button.
	/// </summary>
	/// <param name="text">The text for the button.</param>
	/// <param name="onClicked">The callback that gets invoked when the button is clicked.</param>
	public static void CreateButton(object text, Action onClicked)
	{
		if (GUILayout.Button(text.ToString()))
			onClicked?.Invoke();
	}

	/// <summary>
	/// Adds color to layout code used in the <paramref name="callback"/>.
	/// </summary>
	/// <param name="color">The color hex.</param>
	/// <param name="callback">The code to run within the layout.</param>
	public static void AddColor(string color, Action callback)
	{
		GUI.color = Helpers.ConvertHexToColor(color);
		callback?.Invoke();
		GUI.color = defaultColor;
	}

	/// <summary>
	/// Adds color tags to a string.
	/// </summary>
	/// <param name="text">The text to color.</param>
	/// <param name="color">The hex color for the text.</param>
	/// <returns>The colored text.</returns>
	public static string ColorText(string text, string color)
	{
		return $"<color={color}>{text}</color>";
	}
}