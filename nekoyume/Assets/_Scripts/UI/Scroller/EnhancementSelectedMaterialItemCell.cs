using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class EnhancementSelectedMaterialItemCell : GridCell<Model.EnhancementInventoryItem,
        EnhancementSelectedMaterialItemScroll.ContextModel>
    {
        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private GameObject seletedObj;

        [SerializeField]
        private Button removeButton;

        [SerializeField]
        private TextMeshProUGUI countText;

        private Model.EnhancementInventoryItem item;
        private readonly List<IDisposable> _disposables = new();

        private static readonly int Register = Animator.StringToHash("Register");

        public override void Initialize()
        {
            base.Initialize();
            removeButton.onClick.AddListener(() => Context.OnClick.OnNext(item));
        }

        public override void UpdateContent(Model.EnhancementInventoryItem viewModel)
        {
            item = viewModel;
            if (viewModel?.ItemBase == null)
            {
                animator.SetBool(Register, false);
                seletedObj.SetActive(false);
                return;
            }

            seletedObj.SetActive(true);
            animator.SetBool(Register, true);
            itemImage.overrideSprite = viewModel.ItemBase.GetIconSprite();

            _disposables.DisposeAllAndClear();

            if (ItemEnhancement.HammerIds.Contains(viewModel.ItemBase.Id))
            {
                countText.gameObject.SetActive(true);
                viewModel.SelectedMaterialCount
                    .Subscribe(count => countText.text = count.ToString())
                    .AddTo(_disposables);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
    }
}
