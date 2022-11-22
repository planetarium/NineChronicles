using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class StakingBenefitsListView : MonoBehaviour
    {
        public class Model
        {
            public int BenefitRate;
            public long RequiredDeposit;
            public long HourGlassInterest;
            public long ApPotionInterest;
            public long RuneInterest;
            public int ArenaRewardBuff;
            public int CrystalBuff;
            public int ActionPointBuff;
        }

        [Serializable]
        private class TextList
        {
            public Image iconImage;
            public TextMeshProUGUI benefitRateText;
            public TextMeshProUGUI requiredDepositText;
            public TextMeshProUGUI hourGlassInterestText;
            public TextMeshProUGUI apPotionInterestText;
            public TextMeshProUGUI runeInterestText;
            public TextMeshProUGUI arenaTicketBuffText;
            public TextMeshProUGUI crystalBuffText;
            public TextMeshProUGUI actionPointBuffText;
        }

        [SerializeField] private GameObject none;
        [SerializeField] private GameObject enable;
        [SerializeField] private GameObject select;
        [SerializeField] private GameObject disable;

        [SerializeField] private TextList[] textLists;
        [SerializeField] private StakeIconDataScriptableObject stakeIconData;

        private const string RequiredDepositFormat = "<Style=G0>{0}";

        private const string HourGlassInterestFormat = "<Style=G2>x{0}";
        private const string ApPotionInterestFormat = "<Style=G6>x{0}";
        private const string RuneInterestFormat = "<Style=G9>x{0}";

        private const string ArenaTicketBuffFormat = "<Style=G0>{0}%";
        private const string CrystalBuffFormat = "<Style=G1>{0}%";
        private const string ActionPointBuffFormat = "<Style=G4>{0}% DC";

        public void Set(int modelLevel, Model viewModel)
        {
            var levelIcon = stakeIconData.GetIcon(modelLevel, IconType.Small);
            foreach (var textList in textLists)
            {
                textList.iconImage.sprite = levelIcon;
                textList.benefitRateText.text = $"{viewModel.BenefitRate}%";
                textList.requiredDepositText.text =
                    string.Format(RequiredDepositFormat, viewModel.RequiredDeposit);

                textList.hourGlassInterestText.text =
                    string.Format(HourGlassInterestFormat, viewModel.HourGlassInterest);
                textList.apPotionInterestText.text =
                    string.Format(ApPotionInterestFormat, viewModel.ApPotionInterest);
                textList.runeInterestText.text = viewModel.RuneInterest == 0
                    ? "-"
                    : string.Format(RuneInterestFormat, viewModel.RuneInterest);

                textList.arenaTicketBuffText.text = viewModel.ArenaRewardBuff == 0
                    ? "-"
                    : string.Format(ArenaTicketBuffFormat, viewModel.ArenaRewardBuff);
                textList.crystalBuffText.text = viewModel.CrystalBuff == 0
                    ? "-"
                    : string.Format(CrystalBuffFormat, viewModel.CrystalBuff);
                textList.actionPointBuffText.text = viewModel.ActionPointBuff != 0
                    ? string.Format(ActionPointBuffFormat, viewModel.ActionPointBuff)
                    : "-";
            }
        }

        public void Set(int modelLevel, int currentLevel)
        {
            none.SetActive(modelLevel < 1);
            enable.SetActive(modelLevel >= 1 && modelLevel < currentLevel);
            select.SetActive(modelLevel == currentLevel);
            disable.SetActive(modelLevel > currentLevel);
        }
    }
}
