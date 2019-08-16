using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.TableData;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemInformationSkill : MonoBehaviour
    {
        public Image iconImage;
        public RectTransform informationArea;
        public Text nameText;
        public Text powerText;
        public Text chanceText;
        
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private string _power;
        private string _chance;

        public bool IsShow => gameObject.activeSelf;
        public Model.ItemInformationSkill Model { get; private set; }

        private void Awake()
        {
            _power = LocalizationManager.Localize("UI_POWER");
            _chance = LocalizationManager.Localize("UI_CHANCE");
        }

        public void Show(Model.ItemInformationSkill model)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.iconSprite.SubscribeToImage(iconImage).AddTo(_disposablesForModel);
            Model.name.SubscribeToText(nameText).AddTo(_disposablesForModel);
            Model.power.SubscribeToText(powerText).AddTo(_disposablesForModel);
            Model.chance.SubscribeToText(chanceText).AddTo(_disposablesForModel);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Model.Dispose();
            Model = null;
            _disposablesForModel.DisposeAllAndClear();
        }

        private void Subscribe(SkillSheet.Row row)
        {
            iconImage.sprite = row.GetIcon();
            nameText.text = row.GetLocalizedName();
            powerText.text = $"{_power}: ??";
            chanceText.text = $"{_chance}: ??";
        }
    }
}
