using Nekoyume.Model.State;
using Nekoyume.State;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class BonusBuffButton : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private TextMeshProUGUI starCountText = null;

        private int _stageId;

        private bool _hasEnoughStars;

        private void Awake()
        {
            button.onClick.AddListener(OnClickButton);
        }

        public void SetData(HackAndSlashBuffState state, int currentStageId)
        {
            if (state is null ||
                States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(currentStageId) ||
                !Game.Game.instance.TableSheets.CrystalStageBuffGachaSheet.TryGetValue(state.StageId, out var row))
            {
                gameObject.SetActive(false);
                return;
            }

            _stageId = currentStageId;
            _hasEnoughStars = state.StarCount >= row.MaxStar;
            starCountText.text = $"{state.StarCount}/{row.MaxStar}";
            gameObject.SetActive(true);
        }

        private void OnClickButton()
        {
            var buffState = States.Instance.HackAndSlashBuffState;

            if (!buffState.BuffIds.Any())
            {
                Widget.Find<BuffBonusPopup>().Show(_stageId, _hasEnoughStars);
            }
            else
            {
                Widget.Find<BuffBonusResultPopup>().Show(buffState);
            }
        }
    }
}
