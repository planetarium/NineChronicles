using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class CollectionCell : RectCell<CollectionCell.ViewModel, CollectionScroll.ContextModel>
    {
        public class ViewModel
        {
            public CollectionSheet.Row Row;
            public bool Active;
        }

        [SerializeField] private Button activeButton;

        public override void UpdateContent(ViewModel itemData)
        {
            activeButton.onClick.RemoveAllListeners();
            activeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Context.OnClickActiveButton.OnNext(itemData);
            });

            foreach (var collectionMaterial in itemData.Row.Materials)
            {
                Debug.Log($"collectionMaterial.ItemId: {collectionMaterial.ItemId}\n" +
                          $"collectionMaterial.Level: {collectionMaterial.Level}\n" +
                          $"collectionMaterial.Count: {collectionMaterial.Count}\n");
            }

            foreach (var statModifier in itemData.Row.StatModifiers)
            {
                Debug.Log($"statModifier.StatType: {statModifier.StatType}\n" +
                          $"statModifier.Value: {statModifier.Value}\n");
            }

            activeButton.interactable = !itemData.Active;
        }
    }
}
