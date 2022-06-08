using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StakingPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;

        [SerializeField] private ConditionalButton stakingButton;

        [SerializeField] private Image stepImage;

        [SerializeField] private TextMeshProUGUI stepText;

        [SerializeField] private TextMeshProUGUI stepScoreText;

        [SerializeField] private StakingItemView[] regularItemViews;

        [SerializeField] private GameObject tooltip;

        [SerializeField] private RectTransform tooltipRectTransform;

        [SerializeField] private TextMeshProUGUI tooltipText;

        private const string StepScoreFormat = "{0}<size=18><color=#A36F56>/{1}</color></size>";

        protected override void Awake()
        {
            base.Awake();

            stakingButton.OnSubmitSubject.Subscribe(_ =>
            {
                Debug.LogError("stakingButton.OnSubmitSubject");
            }).AddTo(gameObject);

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
            // StepImage sprite list 불러오기

            tooltip.SetActive(false);
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            stepImage.sprite = null;
            stepText.text = $"Step {4}";
            stepScoreText.text = string.Format(StepScoreFormat, 999, 999);

            foreach (var view in regularItemViews)
            {
                view.Set(null, 100, itemBase =>
                {
                    ShowToolTip(itemBase, view.GetComponent<RectTransform>().anchoredPosition);
                });
                Debug.LogError($"{view.transform.position}");
            }
            base.Show(ignoreStartAnimation);
        }

        private void ShowToolTip(ItemBase itemBase, Vector2 position)
        {
            tooltip.SetActive(true);
            tooltipText.text = itemBase.GetLocalizedDescription();
            tooltipRectTransform.anchoredPosition = position;
        }

        public void OnEnterButtonArea(bool value)
        {
            Debug.LogError(value);
        }
    }
}
