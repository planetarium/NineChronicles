using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ProgressBar : MonoBehaviour
{
	public Sprite greenBar;
	public Sprite redBar;
	public Color greenColor;
	public Color redColor;

	public Image bar;
	public Text label;
	public Text[] labelShadows;

    public void UpdatePosition(GameObject target, Vector3 offset = new Vector3())
    {
		SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
		Vector3 targetPosition = target.transform.position
		+ new Vector3(0.0f, renderer.sprite.rect.size.y / 160)
		+ offset;

		// https://answers.unity.com/questions/799616/unity-46-beta-19-how-to-convert-from-world-space-t.html
		float screenHeight = Screen.height * 0.5f;
		RectTransform canvasRect = transform.root.gameObject.GetComponent<RectTransform>();
		Vector2 viewportPosition = Camera.main.WorldToViewportPoint(targetPosition);
		Vector2 canvasPosition = new Vector2(
			((viewportPosition.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f)),
			((viewportPosition.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f)));
		if (canvasPosition.y > screenHeight)
		{
			float margin = 50.0f;
			canvasPosition.y = screenHeight - margin;
		}
		RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = canvasPosition;
    }

	public void SetText(string text)
	{
		label.text = text;

		if (labelShadows.Length == 0)
			labelShadows = transform.Find("TextShadow").GetComponentsInChildren<Text>();
		foreach (var l in labelShadows)
		{
			l.text = text;
		}
	}

	public void SetValue(float value)
	{
		if (value < 0.1f)
			value = 0.1f;

		Slider slider = gameObject.GetComponent<Slider>();
		if (slider != null)
        	slider.value = value;

		if (value < 0.35f)
		{
			label.color = redColor;
			bar.sprite = redBar;
		}
		else
		{
			label.color = greenColor;
			bar.sprite = greenBar;
		}
	}
}
