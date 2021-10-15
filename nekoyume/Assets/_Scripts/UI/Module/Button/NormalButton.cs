using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class NormalButton : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private string localizationKey = null;

        [SerializeField]
        private Canvas sortingGroup = null;

        private int _originalSortingOrderOffset;

        public readonly Subject<NormalButton> OnClick = new Subject<NormalButton>();

        #region Mono

        protected virtual void Awake()
        {
            if (sortingGroup)
            {
                var widget = GetComponentInParent<Widget>();
                if (widget is HeaderMenuStatic)
                {
                    _originalSortingOrderOffset = 0;
                    sortingGroup.sortingOrder =
                        MainCanvas.instance.GetLayer(widget.WidgetType).root.sortingOrder;
                }
                else if (widget)
                {
                    var layerSortingOrder =
                        MainCanvas.instance.GetLayer(widget.WidgetType).root.sortingOrder;
                    _originalSortingOrderOffset = sortingGroup.sortingOrder - layerSortingOrder;
                }
                else
                {
                    _originalSortingOrderOffset = sortingGroup.sortingOrder;
                }
            }

            text.text =
                L10nManager.Localize(string.IsNullOrEmpty(localizationKey)
                    ? "null"
                    : localizationKey);
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClick.OnNext(this);
            }).AddTo(gameObject);

            sortingGroup.sortingLayerName = "UI";
        }

        #endregion

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetSortOrderToTop()
        {
            if (!sortingGroup)
            {
                return;
            }

            var systemInfoSortingOrder =
                MainCanvas.instance.GetLayer(WidgetType.System).root.sortingOrder;
            sortingGroup.sortingOrder = systemInfoSortingOrder;
        }

        public void SetSortOrderToNormal()
        {
            if (!sortingGroup)
            {
                return;
            }

            var widget = GetComponentInParent<Widget>();
            if (widget)
            {
                var layerSortingOrder =
                    MainCanvas.instance.GetLayer(widget.WidgetType).root.sortingOrder;
                sortingGroup.sortingOrder = layerSortingOrder + _originalSortingOrderOffset;
            }
            else
            {
                sortingGroup.sortingOrder = _originalSortingOrderOffset;
            }
        }
    }
}
