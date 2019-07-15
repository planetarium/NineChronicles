using System;
using System.Collections;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoginPlayerSlot : MonoBehaviour
    {
        public GameObject NameView;
        public Text LabelLevel;
        public Image Icon;
        public Text LabelName;
        public GameObject CreateView;
        public Text CreateViewText;
        public GameObject DeleteView;
        public Text DeleteViewButtonText;

        private void Awake()
        {
            CreateViewText.text = LocalizationManager.Localize("UI_CREATE_CHARACTER");
            DeleteViewButtonText.text = LocalizationManager.Localize("UI_DELETE");
        }
    }
}
