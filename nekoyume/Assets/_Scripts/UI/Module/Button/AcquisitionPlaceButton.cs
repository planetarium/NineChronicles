using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using System;

    public class AcquisitionPlaceButton : MonoBehaviour
    {
        public class Model
        {
            public Model(ShortcutHelper.PlaceType type,
                Action onClick,
                string guideText,
                int stageId = 0)
            {
                Type = type;
                OnClick = onClick;
                GuideText = guideText;
                StageId = stageId;
            }

            public ShortcutHelper.PlaceType Type { get; }
            public Action OnClick { get; }
            public string GuideText { get; }
            public int StageId { get; }
        }

        [SerializeField]
        private Button button;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI guideText;

        [SerializeField]
        private TextMeshProUGUI lockedText;

        [SerializeField]
        private GameObject enableObject;

        [SerializeField]
        private GameObject disableObject;

        private static Dictionary<string, Sprite> _iconDictionary;

        private const string IconNameFormat = "icon_Navigation_{0}";

        private const string MimisbrunnrIconIndex = "006";

        public void Set(Model model)
        {
            lockedText.text = guideText.text = model.GuideText;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (ShortcutHelper.CheckUIStateForUsingShortcut(model.Type) &&
                    !disableObject.activeSelf)
                {
                    model.OnClick?.Invoke();
                }
            });
            enableObject.SetActive(false);
            disableObject.SetActive(false);

            string iconName = string.Empty;
            switch (model.Type)
            {
                case ShortcutHelper.PlaceType.Stage:
                    if (Game.Game.instance.TableSheets.WorldSheet.
                        TryGetByStageId(model.StageId, out var worldRow))
                    {
                        iconName = string.Format(IconNameFormat, (100 + worldRow.Id).ToString());
                    }

                    break;
                case ShortcutHelper.PlaceType.PCShop:
                case ShortcutHelper.PlaceType.Arena:
                case ShortcutHelper.PlaceType.Quest:
                case ShortcutHelper.PlaceType.Staking:
                case ShortcutHelper.PlaceType.EventDungeonStage:
                case ShortcutHelper.PlaceType.Craft:
                case ShortcutHelper.PlaceType.Summon:
                    iconName = string.Format(IconNameFormat, $"{(int)model.Type:000}");
                    break;
                case ShortcutHelper.PlaceType.MobileShop:
                    iconName = string.Format(IconNameFormat, $"00{(int)ShortcutHelper.PlaceType.PCShop}");
                    break;
                case ShortcutHelper.PlaceType.Upgrade:
                    iconName = string.Format(IconNameFormat, $"00{(int)ShortcutHelper.PlaceType.Craft}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_iconDictionary.TryGetValue(iconName, out var sprite))
            {
                iconImage.sprite = sprite;
            }

            EnableSettingByPlaceType(model.Type, model);
        }

        private void EnableSettingByPlaceType(ShortcutHelper.PlaceType type, Model model)
        {
            var enable = ShortcutHelper.CheckConditionOfShortcut(type, model.StageId);
            enableObject.SetActive(enable);
            disableObject.SetActive(!enable);
        }

        private void Awake()
        {
            _iconDictionary ??= Resources.LoadAll<Sprite>("UI/Icons/Navigation/").ToDictionary(image => image.name);
        }
    }
}
