using System;
using System.Globalization;
using Nekoyume.Game;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Arena.Board
{
    using UniRx;

    [Serializable]
    public class ArenaBoardPlayerItemData
    {
        public string name;
        public int level;
        public int fullCostumeOrArmorId;
        public int? titleId;
        public int cp;
        public int score;
        public int rank;
        public int expectWinDeltaScore;
        public bool interactableChoiceButton;
        public bool canFight;
        public string address;
        public string guildName;
    }

    public class ArenaBoardPlayerScrollContext : FancyScrollRectContext
    {
        public int selectedIndex = -1;
        public Action<int> onClickCharacterView;
        public Action<int> onClickChoice;
    }

    public class ArenaBoardPlayerCell
        : FancyScrollRectCell<ArenaBoardPlayerItemData, ArenaBoardPlayerScrollContext>
    {
        [SerializeField]
        private Image _rankImage;

        [SerializeField]
        private GameObject _rankImageContainer;

        [SerializeField]
        private TextMeshProUGUI _rankText;

        [SerializeField]
        private GameObject _rankTextContainer;

        [SerializeField]
        private DetailedCharacterView _characterView;

        [SerializeField]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        private TextMeshProUGUI _ratingText;

        [SerializeField]
        private TextMeshProUGUI _cpText;

        [SerializeField]
        private TextMeshProUGUI _plusRatingText;

        [SerializeField]
        private ConditionalButton _choiceButton;

        [SerializeField]
        private Image guildMark;

        [SerializeField]
        private Image guildMarkEmpty;

        private ArenaBoardPlayerItemData _currentData;

#if UNITY_EDITOR
        [ReadOnly]
        public float _normalizedPosition;
#else
        private float _normalizedPosition;
#endif

        private void Awake()
        {
            _characterView.OnClickCharacterIcon
                .Subscribe(_ => Context.onClickCharacterView?.Invoke(Index))
                .AddTo(gameObject);

            _choiceButton.OnClickSubject
                .Subscribe(_ => Context.onClickChoice?.Invoke(Index))
                .AddTo(gameObject);
        }

        public override void UpdateContent(ArenaBoardPlayerItemData itemData)
        {
            _currentData = itemData;

            var prefixedAddress = "0x" + _currentData.address;
            if (Dcc.instance.Avatars.TryGetValue(prefixedAddress, out var dccId))
            {
                _characterView.SetByDccId(dccId, _currentData.level);
            }
            else
            {
                _characterView.SetByFullCostumeOrArmorId(
                    _currentData.fullCostumeOrArmorId,
                    _currentData.level.ToString("N0", CultureInfo.CurrentCulture));
            }

            _nameText.text = _currentData.name;
            _cpText.text = _currentData.cp.ToString("N0", CultureInfo.CurrentCulture);
            _ratingText.text = _currentData.score.ToString("N0", CultureInfo.CurrentCulture);
            _plusRatingText.gameObject.SetActive(_currentData.canFight);
            _plusRatingText.text = _currentData.expectWinDeltaScore.ToString("N0", CultureInfo.CurrentCulture);

            _choiceButton.gameObject.SetActive(_currentData.canFight);
            _choiceButton.Interactable = _currentData.interactableChoiceButton;
            var guildEnabled = !string.IsNullOrEmpty(_currentData.guildName);
            if (guildEnabled)
            {
                var url = $"{Game.Game.instance.GuildBucketUrl}/{_currentData.guildName}.png";
                guildMark.sprite = Util.GetTexture(url);
                NcDebug.Log($"[Guild]Set guild image {_currentData.guildName}");
            }

            guildMark.enabled = guildEnabled;
            guildMarkEmpty.enabled = !guildEnabled;

            UpdateRank();
        }

        protected override void UpdatePosition(float normalizedPosition, float localPosition)
        {
            _normalizedPosition = normalizedPosition;
            base.UpdatePosition(_normalizedPosition, localPosition);
        }

        private void UpdateRank()
        {
            switch (_currentData.rank)
            {
                case -1:
                    _rankImageContainer.SetActive(false);
                    _rankText.text = "-";
                    _rankTextContainer.SetActive(true);
                    break;
                case 1:
                case 2:
                case 3:
                    _rankImage.overrideSprite = SpriteHelper.GetRankIcon(_currentData.rank);
                    _rankImageContainer.SetActive(true);
                    _rankTextContainer.SetActive(false);
                    break;
                default:
                    _rankImageContainer.SetActive(false);
                    _rankText.text =
                        _currentData.rank.ToString("N0", CultureInfo.CurrentCulture);
                    _rankTextContainer.SetActive(true);
                    break;
            }
        }
    }
}
