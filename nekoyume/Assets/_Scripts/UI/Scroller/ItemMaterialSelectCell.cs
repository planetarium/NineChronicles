using Nekoyume.Helper;
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

        private static readonly Color DefaultColor = ColorHelper.HexToColorRGB("ebceb1");
        private static readonly Color DimmedColor = ColorHelper.HexToColorRGB("292520");

        public override void UpdateContent(ItemMaterialSelectScroll.Model model)
        {
            _model = model;
            itemView.SetData(model.Item);
            itemNameText.text = model.Item.ItemBase.Value.GetLocalizedName();

            countInputField.onValueChanged.RemoveAllListeners();
            increaseCountButton.onClick.RemoveAllListeners();
            decreaseCountButton.onClick.RemoveAllListeners();

            if (model.Item.Count.Value > 0)
            {
                countInputField.text = _model.SelectedCount.Value.ToString();
                countInputField.textComponent.color = DefaultColor;
                countInputField.interactable = true;
                increaseCountButton.gameObject.SetActive(true);
                decreaseCountButton.gameObject.SetActive(true);

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
            else
            {
                countInputField.text = "-";
                countInputField.textComponent.color = DimmedColor;
                countInputField.interactable = false;
                increaseCountButton.gameObject.SetActive(false);
                decreaseCountButton.gameObject.SetActive(false);
            }
        }

        private void OnChangeCount(int count)
        {
            Context.OnChangeCount.OnNext((_model, count));
        }
    }
}
