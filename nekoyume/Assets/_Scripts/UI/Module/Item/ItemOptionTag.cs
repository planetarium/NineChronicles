using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemOptionTag : MonoBehaviour
    {
        [SerializeField]
        private OptionTagDataScriptableObject optionTagData = null;

        [SerializeField]
        private UIHsvModifier background;

        [SerializeField]
        private List<Image> optionTagImages = null;

        public void Set(ItemBase itemBase)
        {
            background.gameObject.SetActive(false);

            if (!(itemBase is Equipment equipment))
            {
                return;
            }

            if (equipment.GetOptionCountFromCombination() <= 0)
            {
                return;
            }

            var data = optionTagData.GetOptionTagData(itemBase.Grade);
            foreach (var image in optionTagImages)
            {
                image.gameObject.SetActive(false);
            }

            background.range = data.GradeHsvRange;
            background.hue = data.GradeHsvHue;
            background.saturation = data.GradeHsvSaturation;
            background.value = data.GradeHsvValue;
            var optionInfo = new ItemOptionInfo(equipment);

            var optionCount = optionInfo.StatOptions.Sum(x => x.count);
            var index = 0;
            for (var i = 0; i < optionCount; ++i)
            {
                var image = optionTagImages[index];
                image.gameObject.SetActive(true);
                image.sprite = optionTagData.StatOptionSprite;
                ++index;
            }

            for (var i = 0; i < optionInfo.SkillOptions.Count; ++i)
            {
                var image = optionTagImages[index];
                image.gameObject.SetActive(true);
                image.sprite = optionTagData.SkillOptionSprite;
                ++index;
            }

            background.gameObject.SetActive(true);
        }

        public void Set(int grade)
        {
            var data = optionTagData.GetOptionTagData(grade);
            foreach (var image in optionTagImages)
            {
                image.gameObject.SetActive(false);
            }

            var first = optionTagImages.First();
            first.gameObject.SetActive(true);
            first.sprite = optionTagData.SkillOptionSprite;

            background.range = data.GradeHsvRange;
            background.hue = data.GradeHsvHue;
            background.saturation = data.GradeHsvSaturation;
            background.value = data.GradeHsvValue;
            background.gameObject.SetActive(true);
        }
    }
}
