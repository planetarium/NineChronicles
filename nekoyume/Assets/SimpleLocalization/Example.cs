using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.SimpleLocalization
{
	/// <summary>
	/// Asset usage example.
	/// </summary>
	public class Example : MonoBehaviour
	{
		public Text FormattedText;

		/// <summary>
		/// Called on app start.
		/// </summary>
		public void Awake()
		{
			LocalizationManager.Read();

			switch (Application.systemLanguage)
			{
				case SystemLanguage.German:
					LocalizationManager.Language = "German";
					break;
				case SystemLanguage.Russian:
					LocalizationManager.Language = "Russian";
					break;
				default:
					LocalizationManager.Language = "English";
					break;
			}

			// This way you can insert values to localized strings.
			FormattedText.text = LocalizationManager.Localize("Settings.PlayTime", TimeSpan.FromHours(10.5f).TotalHours);

			// This way you can subscribe to localization changed event.
			LocalizationManager.LocalizationChanged += () => FormattedText.text = LocalizationManager.Localize("Settings.PlayTime", TimeSpan.FromHours(10.5f).TotalHours);
		}

		/// <summary>
		/// Change localization at runtime
		/// </summary>
		public void SetLocalization(string localization)
		{
			LocalizationManager.Language = localization;
		}

		/// <summary>
		/// Write a review.
		/// </summary>
		public void Review()
		{
			Application.OpenURL("https://www.assetstore.unity3d.com/#!/content/120113");
		}
	}
}