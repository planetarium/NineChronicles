using System;
using System.Globalization;
using TMPro;
using UnityEngine;
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
        public int expectWinDeltaScore;
        public bool interactableChoiceButton;
    }

    public class ArenaBoardPlayerScrollContext : FancyScrollRectContext
    {
        public int selectedIndex = -1;
        public Action<int> onClickChoice;
    }

    public class ArenaBoardPlayerCell
        : FancyScrollRectCell<ArenaBoardPlayerItemData, ArenaBoardPlayerScrollContext>
    {
        [SerializeField] private DetailedCharacterView _characterView;

        [SerializeField] private TextMeshProUGUI _nameText;

        [SerializeField] private TextMeshProUGUI _ratingText;

        [SerializeField] private TextMeshProUGUI _cpText;

        [SerializeField] private TextMeshProUGUI _plusRatingText;

        [SerializeField] private ConditionalButton _choiceButton;

        private ArenaBoardPlayerItemData _currentData;

#if UNITY_EDITOR
        [ReadOnly]
        public float _normalizedPosition;
#else
        private float _normalizedPosition;
#endif

        private void Awake()
        {
            _choiceButton.OnClickSubject
                .Subscribe(_ => Context.onClickChoice?.Invoke(Index))
                .AddTo(gameObject);
        }

        public override void UpdateContent(ArenaBoardPlayerItemData itemData)
        {
            _currentData = itemData;
            _characterView.SetByFullCostumeOrArmorId(
                _currentData.fullCostumeOrArmorId,
                _currentData.titleId,
                _currentData.level);
            _nameText.text = _currentData.name;
            _cpText.text = _currentData.cp.ToString("N0", CultureInfo.CurrentCulture);
            _ratingText.text = _currentData.score.ToString("N0", CultureInfo.CurrentCulture);
            _plusRatingText.text
                = _currentData.expectWinDeltaScore.ToString("N0", CultureInfo.CurrentCulture);
            _choiceButton.Interactable = _currentData.interactableChoiceButton;
        }

        protected override void UpdatePosition(float normalizedPosition, float localPosition)
        {
            _normalizedPosition = normalizedPosition;
            base.UpdatePosition(_normalizedPosition, localPosition);
        }
    }
}