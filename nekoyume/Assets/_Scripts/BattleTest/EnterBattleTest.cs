using Nekoyume.Blockchain;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using Nekoyume.Model.EnumType;

namespace Nekoyume.BattleTest
{
    using UniRx;

    /// <summary>
    /// 씬 전환 테스트용으로 작성한 스크립트
    /// 테스트 이후 씬 이동 로직 안정화되면 삭제예정
    /// </summary>
    public class EnterBattleTest : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField worldIdInputField;

        [SerializeField]
        private TMP_InputField stageIdInputField;

        public void EnterBattle()
        {
            var worldId = int.Parse(worldIdInputField.text);
            var stageId = int.Parse(stageIdInputField.text);

            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Adventure];
            var costumes = itemSlotState.Costumes;
            var equipments = itemSlotState.Equipments;
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure]
                                  .GetEquippedRuneSlotInfos();

            ActionManager.Instance.HackAndSlash(
                costumes,
                equipments,
                null,
                runeInfos,
                worldId,
                stageId,
                playCount: 1,
                apStoneCount: 0,
                trackGuideQuest: false
            ).Subscribe();
        }
    }
}
