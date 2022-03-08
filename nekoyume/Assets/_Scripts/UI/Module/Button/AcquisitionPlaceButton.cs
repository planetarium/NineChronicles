using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using System;
    public class AcquisitionPlaceButton : MonoBehaviour
    {
        public enum PlaceType
        {
            Stage,
            Shop = 3,
            Arena = 4,
            Quest = 5,
            Staking = 7
        }

        public class Model
        {
            public Model(PlaceType type, Action onClick, string guideText, ItemBase itemBase, WorldSheet.Row worldRow = null)
            {
                Type = type;
                OnClick = onClick;
                GuideText = guideText;
                ItemBase = itemBase;
                WorldRow = worldRow;
            }

            public PlaceType Type { get; }
            public Action OnClick { get; }
            public string GuideText { get; }
            public ItemBase ItemBase { get; }
            public WorldSheet.Row WorldRow { get; }
        }

        [SerializeField]
        private Button button;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI guideText;

        [SerializeField]
        private TextMeshProUGUI lockedText;

        private Model _model;

        private Action _onClick;

        private static Dictionary<string, Sprite> _iconDictionary;

        private const string IconNameFormat = "icon_Navigation_{0}";

        public void Set(Model model)
        {
            _model = model;
            lockedText.text = guideText.text = model.GuideText;
            _onClick = model.OnClick;

            switch (model.Type)
            {
                case PlaceType.Stage:
                    if (_iconDictionary.TryGetValue(
                            string.Format(
                                IconNameFormat,
                                model.WorldRow.Id < 10000
                                    ? (100 + model.WorldRow.Id).ToString()
                                    : "006"),
                            out var icon))
                    {
                        iconImage.sprite = icon;
                    }
                    break;
                case PlaceType.Shop:
                case PlaceType.Arena:
                case PlaceType.Quest:
                case PlaceType.Staking:
                    if (_iconDictionary.TryGetValue(
                            string.Format(IconNameFormat, $"00{(int) model.Type}"),
                            out var sprite))
                    {
                        iconImage.sprite = sprite;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Awake()
        {
            button.onClick.AddListener(() => _onClick?.Invoke());

            if (_iconDictionary == null)
            {
                _iconDictionary = Resources.LoadAll<Sprite>("UI/Icons/Navigation/").ToDictionary(image => image.name);
            }
        }
    }
}
