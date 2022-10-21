using System;
using Libplanet.Assets;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Nekoyume.State.Subjects;
    using UniRx;

    public class RuneStone: AlphaAnimateModule
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI count;

        [SerializeField]
        private GameObject loadingObject;

        public void SetActiveLoading(bool value)
        {
            loadingObject.SetActive(value);
            count.gameObject.SetActive(!value);
        }

        public void SetRuneStone(Sprite icon, string quantity)
        {
            iconImage.sprite = icon;
            this.count.text = quantity;
        }
    }
}
