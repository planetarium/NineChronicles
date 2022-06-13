using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StakingPopupNone : PopupWidget
    {
        [SerializeField] private TextMeshProUGUI startStakingUnitText;
        [SerializeField] private ConditionalButton uploadButton;
        [SerializeField] private Button closeButton;

        protected override void Awake()
        {
            base.Awake();

            Game.Game.instance.TableSheets.StakeRegularRewardSheet.TryGetValue(1, out var row);
            startStakingUnitText.text = (row?.RequiredGold ?? 0).ToString();

            uploadButton.OnSubmitSubject.Subscribe(_ =>
            {
                // Do Something
                AudioController.PlayClick();
            }).AddTo(gameObject);

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
        }
    }
}
