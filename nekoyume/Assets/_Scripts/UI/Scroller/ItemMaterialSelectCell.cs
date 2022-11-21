using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class ItemMaterialSelectCell : RectCell<ItemMaterialSelectScroll.Model, ItemMaterialSelectScroll.ContextModel>
    {
        [SerializeField] private SimpleCountableItemView itemView;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TMP_InputField countInputField;
        [SerializeField] private Button increaseCountButton;
        [SerializeField] private Button decreaseCountButton;

        private ItemMaterialSelectScroll.Model _model;

        public override void UpdateContent(ItemMaterialSelectScroll.Model model)
        {
            _model = model;
            itemView.SetData(model.Item);
            itemNameText.text = model.Item.ItemBase.Value.GetLocalizedName();

            countInputField.onValueChanged.RemoveAllListeners();
            increaseCountButton.onClick.RemoveAllListeners();
            decreaseCountButton.onClick.RemoveAllListeners();

            countInputField.onValueChanged.AddListener(count =>
            {
                OnChangeCount(int.TryParse(count, out var countValue) ? countValue : 0);
                countInputField.text = _model.SelectedCount.Value.ToString();
            });
            increaseCountButton.onClick.AddListener(() => OnChangeCount(_model.SelectedCount.Value + 1));
            decreaseCountButton.onClick.AddListener(() => OnChangeCount(_model.SelectedCount.Value - 1));

            _model.SelectedCount
                .Subscribe(count => countInputField.text = count.ToString())
                .AddTo(gameObject);
        }

        private void OnChangeCount(int count)
        {
            Context.OnChangeCount.OnNext((_model, count));
        }
    }
}
