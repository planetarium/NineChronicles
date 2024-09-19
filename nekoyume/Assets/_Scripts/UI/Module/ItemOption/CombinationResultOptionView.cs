using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CombinationResultOptionView : ItemOptionView
    {        
        [SerializeField] 
        private TMP_Text ratingText;
        
        [SerializeField]
        private GameObject top10Object;
        
        [SerializeField]
        private TMP_Text coverText;

        public bool IsEmpty { get; private set; } = true;
        
        public void UpdateAsStatWithCount(StatType type, long value, int topPercent)
        {
            IsEmpty = false;
            
            UpdateView($"{type}", $" +{type.ValueToString(value)}");
            
            ratingText.text = $"Top {topPercent}%";
            top10Object.SetActive(topPercent <= 10);

            coverText.text = $"{type} Get Random Stats";
        }

        public void UpdateAsSkill(string skillName, string powerString, int totalChance, int topPercent)
        {
            IsEmpty = false;
            
            UpdateView($"{skillName} {powerString} / {totalChance}%", string.Empty);
            
            ratingText.text = $"Top {topPercent}%";
            top10Object.SetActive(topPercent <= 10);
            
            coverText.text = $"{skillName} Get Random Skill";
        }
        
        public override void UpdateToEmpty()
        {
            IsEmpty = true;
            base.UpdateToEmpty();

            coverText.text = string.Empty;
        }
    }
}
