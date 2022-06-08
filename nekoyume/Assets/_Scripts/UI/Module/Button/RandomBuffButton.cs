using Nekoyume.Model.State;
using Nekoyume.State;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RandomBuffButton : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI starCountText = null;

        public void SetData(HackAndSlashBuffState state, int currentStageId)
        {
            if (state is null ||
                States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(currentStageId) ||
                !Game.Game.instance.TableSheets.CrystalStageBuffGachaSheet.TryGetValue(state.StageId, out var row))
            {
                gameObject.SetActive(false);
                return;
            }

            starCountText.text = $"{state.StarCount}/{row.MaxStar}";
            gameObject.SetActive(true);
        }
    }
}
