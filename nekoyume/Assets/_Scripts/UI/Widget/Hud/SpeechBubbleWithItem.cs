using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SpeechBubbleWithItem : SpeechBubble
    {
        [Serializable]
        private struct CurrencyView
        {
            public GameObject view;
            public TMP_Text amountText;
        }

        [SerializeField]
        private SimpleItemView itemView = null;

        [SerializeField]
        private CurrencyView ncgView;

        [SerializeField]
        private CurrencyView crystalView;

        [SerializeField]
        private GameObject runeView;

        [SerializeField]
        private Image runeImage;

        private bool _showItemView;
        private bool _showNCGView;
        private bool _showCrystalView;
        private bool _showRune;

        public Item Item { get; private set; }
        public SimpleItemView ItemView => itemView;
        public Image RuneImage => runeImage;

        public void SetItemMaterial(Item item, bool isConsumable)
        {
            Item = item;
            itemView.SetData(item, isConsumable);

            _showItemView = true;
            _showNCGView = false;
            _showCrystalView = false;
            _showRune = false;
        }

        public void SetCurrency(long ncg, long crystal)
        {
            Item = null;
            var hasNCG = ncg > 0;
            var hasCrystal = crystal > 0;

            if (hasNCG)
            {
                ncgView.amountText.text = ncg.ToString();
            }

            if (hasCrystal)
            {
                crystalView.amountText.text = crystal.ToString();
            }

            _showItemView = false;
            _showNCGView = hasNCG;
            _showCrystalView = hasCrystal;
            _showRune = false;
        }

        public void SetRune(Sprite runeIcon)
        {
            _showItemView = false;
            _showNCGView = false;
            _showCrystalView = false;
            _showRune = true;
            runeImage.sprite = runeIcon;
        }

        public override void Hide()
        {
            base.Hide();

            itemView.Hide();
            ncgView.view.SetActive(false);
            crystalView.view.SetActive(false);
        }

        protected override void SetBubbleImageInternal()
        {
            base.SetBubbleImageInternal();

            if (_showItemView)
            {
                itemView.Show();
            }

            ncgView.view.SetActive(_showNCGView);
            crystalView.view.SetActive(_showCrystalView);
            runeView.SetActive(_showRune);
        }
    }
}
