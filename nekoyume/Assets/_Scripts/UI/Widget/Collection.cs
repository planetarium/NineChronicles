using System.Collections.Generic;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class Collection : Widget
    {
        public class Model
        {
            public CollectionSheet.Row Row;
            public bool Active;
            // search tags
        }

        public static List<Model> GetModels()
        {
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionState = Game.Game.instance.States.CollectionState;
            var models = new List<Model>();
            foreach (var row in collectionSheet.Values)
            {
                var active = collectionState.Ids.Contains(row.Id);
                models.Add(new Model
                {
                    Row = row,
                    Active = active
                });
            }

            return models;
        }

        [SerializeField] private Button backButton;
        [SerializeField] private CollectionEffect collectionEffect;
        [SerializeField] private CollectionScroll scroll;
        // Todo : Item Info View

        private CollectionMaterial _selectedMaterial;

        protected override void Awake()
        {
            base.Awake();

            backButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };

            scroll.OnClickActiveButton
                .Subscribe(ActivateCollectionAction)
                .AddTo(gameObject);
            scroll.OnClickMaterial
                .Subscribe(OnClickMaterial)
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            UpdateView();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
        }

        private void UpdateView()
        {
            var models = GetModels();
            scroll.UpdateData(models, true);
            collectionEffect.Set(models.ToArray());
        }

        private void OnClickMaterial(CollectionMaterial viewModel)
        {
            if (_selectedMaterial == null)
            {
                _selectedMaterial = viewModel;
                _selectedMaterial.Selected.SetValueAndForceNotify(true);
            }
            else if (_selectedMaterial.Equals(viewModel))
            {
                _selectedMaterial.Selected.SetValueAndForceNotify(false);
                _selectedMaterial = null;
            }
            else
            {
                _selectedMaterial.Selected.SetValueAndForceNotify(false);
                _selectedMaterial = viewModel;
                _selectedMaterial.Selected.SetValueAndForceNotify(true);
            }

            // Todo : Show Item Info
        }

        private void ActivateCollectionAction(Model model)
        {
            // check collection - is active
            var collectionState = Game.Game.instance.States.CollectionState;
            if (collectionState.Ids.Contains(model.Row.Id))
            {
                Debug.LogError("collection already active");
                return;
            }

            // set materials
            Find<CollectionRegistrationPopup>().Show(model, materials =>
            {
                var collectionId = model.Row.Id;
                ActionManager.Instance.ActivateCollection(collectionId, materials).Subscribe();
                LoadingHelper.ActivateCollection.Value = true;
            });
        }

        public void OnActionRender()
        {
            LoadingHelper.ActivateCollection.Value = false;
            UpdateView();
        }
    }
}
