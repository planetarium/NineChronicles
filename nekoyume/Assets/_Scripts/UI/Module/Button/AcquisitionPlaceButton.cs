using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.State;
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
            public Model(PlaceType type, Action onClick, string guideText, ItemBase itemBase, StageSheet.Row stageRow = null)
            {
                Type = type;
                OnClick = onClick;
                GuideText = guideText;
                ItemBase = itemBase;
                StageRow = stageRow;
            }

            public PlaceType Type { get; }
            public Action OnClick { get; }
            public string GuideText { get; }
            public ItemBase ItemBase { get; }
            public StageSheet.Row StageRow { get; }
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

        private Model _model;

        private static Dictionary<string, Sprite> _iconDictionary;

        private const string IconNameFormat = "icon_Navigation_{0}";

        private const string MimisbrunnrIconIndex = "006";

        public void Set(Model model)
        {
            _model = model;
            lockedText.text = guideText.text = model.GuideText;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (CanGoToAcquisitionPlace(model.Type))
                {
                    model.OnClick?.Invoke();
                }
            });
            enableObject.SetActive(false);
            disableObject.SetActive(false);

            switch (model.Type)
            {
                case PlaceType.Stage:
                    if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(model.StageRow.Id,
                            out var worldRow))
                    {
                        if (_iconDictionary.TryGetValue(
                                string.Format(
                                    IconNameFormat,
                                    worldRow.Id < 10000
                                        ? (100 + worldRow.Id).ToString()
                                        : MimisbrunnrIconIndex),
                                out var icon))
                        {
                            iconImage.sprite = icon;
                        }
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

                    enableObject.SetActive(true);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EnableSettingByPlaceType(model.Type, model);
        }

        private void EnableSettingByPlaceType(PlaceType type, Model model)
        {
            switch (type)
            {
                case PlaceType.Stage:
                    if (States.Instance.CurrentAvatarState.worldInformation
                        .TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
                    {
                        if (model.StageRow.Id > world.StageClearedId + 1)
                        {
                            disableObject.SetActive(true);
                        }
                        else
                        {
                            enableObject.SetActive(true);
                        }
                    }

                    break;
                case PlaceType.Shop:
                    if (States.Instance.CurrentAvatarState.level <
                        GameConfig.RequireClearedStageLevel.UIMainMenuShop)
                    {
                        disableObject.SetActive(true);
                    }
                    else
                    {
                        enableObject.SetActive(true);
                    }

                    break;
                case PlaceType.Arena:
                    if (States.Instance.CurrentAvatarState.level <
                        GameConfig.RequireClearedStageLevel.UIMainMenuRankingBoard)
                    {
                        disableObject.SetActive(true);
                    }
                    else
                    {
                        enableObject.SetActive(true);
                    }

                    break;
                case PlaceType.Quest:
                case PlaceType.Staking:
                    enableObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static bool CanGoToAcquisitionPlace(PlaceType type)
        {
            return type switch
            {
                PlaceType.Stage => !Game.Game.instance.Stage.IsInStage,
                PlaceType.Shop => !Game.Game.instance.Stage.IsInStage,
                PlaceType.Arena => !Game.Game.instance.Stage.IsInStage,
                PlaceType.Quest => !Widget.Find<BattleResultPopup>().IsActive() && !Widget.Find<RankingBattleResultPopup>().IsActive(),
                PlaceType.Staking => true,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private void Awake()
        {
            _iconDictionary ??= Resources.LoadAll<Sprite>("UI/Icons/Navigation/").ToDictionary(image => image.name);
        }
    }
}
