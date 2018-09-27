using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ProgressBar : MonoBehaviour
{
	public Image bar;
	public Text label;

    public void UpdatePosition(GameObject target, Vector3 offset = new Vector3())
    {
		SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
		Vector3 targetPosition = target.transform.position
		+ new Vector3(0.0f, renderer.sprite.rect.size.y / 100)
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
			float margin = 30.0f;
			canvasPosition.y = screenHeight - margin;
		}
		RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = canvasPosition;
    }
}
