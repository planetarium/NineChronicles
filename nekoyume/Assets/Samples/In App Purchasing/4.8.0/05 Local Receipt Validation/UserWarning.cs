using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.LocalReceiptValidation
{
    public class UserWarning : MonoBehaviour
    {
        public Text warningText;

        public void Clear()
        {
            warningText.text = "";
        }

        public void WarnInvalidStore(AppStore currentAppStore)
        {
            var warningMsg = $"The cross-platform validator is not implemented for the currently selected store: {currentAppStore}. \n" +
                             "Build the project for Android, iOS, macOS, or tvOS and use the Google Play Store or Apple App Store. See README for more information.";
            Debug.LogWarning(warningMsg);
            warningText.text = warningMsg;
        }
    }
}
