using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using TMPro;
using Nekoyume.Model.Stat;
using UnityEngine.UI;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class EquipmentRecipeCellView : MonoBehaviour
    {
        public Button button;
        public Image[] elementalTypeImages;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI optionText;
        public SimpleCountableItemView itemView;

        public Equipment model;

        public readonly Subject<EquipmentRecipeCellView> OnClick = new Subject<EquipmentRecipeCellView>();

        private void Awake()
        {
            button.onClick.AddListener(() =>
            {
                OnClick.OnNext(this);
            });
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Set(Equipment equipment)
        {
            model = equipment;

            titleText.text = equipment.GetLocalizedName();

            var item = new CountableItem(model, 1);
            itemView.SetData(item);

            var data = equipment.Data;
            var sprite = data.ElementalType.GetSprite();
            var grade = data.Grade;

            for (int i = 0; i < elementalTypeImages.Length; ++i)
            {
                if (sprite is null || i >= grade)
                {
                    elementalTypeImages[i].gameObject.SetActive(false);
                    continue;
                }

                elementalTypeImages[i].sprite = sprite;
                elementalTypeImages[i].gameObject.SetActive(true);
            }

            StatType statKey = model.UniqueStatType;
            int statValue = model.StatsMap.GetStat(model.UniqueStatType);
            string text = $"{statKey} +{statValue}";
            optionText.text = text;
        }
    }
}
