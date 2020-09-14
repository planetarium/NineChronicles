using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Extension;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.UI
{
    public class ItemInformationTooltip : VerticalTooltipWidget<Model.ItemInformationTooltip>
    {
        public TextMeshProUGUI titleText;
        public Module.ItemInformation itemInformation;
        public GameObject footerRoot;
        public GameObject submitGameObject;
        public GameObject submitGameObjectForRetrieve;
        public SubmitButton submitButton;
        public SubmitButton submitButtonForRetrieve;
        public GameObject priceContainer;
        public TextMeshProUGUI priceText;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public new Model.ItemInformationTooltip Model { get; private set; }

        public RectTransform Target => Model.target.Value;

        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;

        protected override void Awake()
        {
            base.Awake();

            Model = new Model.ItemInformationTooltip();
            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Model.OnSubmitClick.OnNext(this);
                Close();
            }).AddTo(gameObject);

            submitButtonForRetrieve.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Model.OnSubmitClick.OnNext(this);
                Close();
            }).AddTo(gameObject);

            CloseWidget = () =>
            {
                Model.OnCloseClick.OnNext(this);
                Close();
            };

            SubmitWidget = () =>
            {
                if (!submitButton.IsSubmittable && !submitButtonForRetrieve.IsSubmittable)
                    return;
                AudioController.PlayClick();
                Model.OnSubmitClick.OnNext(this);
                Close();
            };
        }

        protected override void OnDestroy()
        {
            Model.Dispose();
            Model = null;
            base.OnDestroy();
        }

        public void Show(RectTransform target, CountableItem item, Action<ItemInformationTooltip> onClose = null)
        {
            Show(target, item, null, null, null, onClose);
        }

        public void Show(RectTransform target, CountableItem item, Func<CountableItem, bool> submitEnabledFunc,
            string submitText, Action<ItemInformationTooltip> onSubmit, Action<ItemInformationTooltip> onClose = null,
            bool retrieve = false)
        {
            if (item is null)
            {
                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.SetValueAndForceNotify(submitEnabledFunc);
            Model.SubmitButtonText.Value = submitText;

            // Show(Model)을 먼저 호출함으로써 Widget.Show()가 호출되고, 게임 오브젝트가 활성화 됨. 그래야 레이아웃 정리가 가능함.
            Show(Model);
            // itemInformation UI의 모든 요소에 적절한 값이 들어가야 레이아웃 정리가 유효함.
            itemInformation.SetData(Model.ItemInformation);

            Model.TitleText.SubscribeTo(titleText).AddTo(_disposablesForModel);
            Model.PriceEnabled.Subscribe(priceContainer.SetActive).AddTo(_disposablesForModel);
            Model.PriceEnabled.SubscribeTo(priceText).AddTo(_disposablesForModel);
            Model.Price.SubscribeToPrice(priceText).AddTo(_disposablesForModel);

            if (retrieve)
            {
                Model.SubmitButtonText.SubscribeTo(submitButtonForRetrieve).AddTo(_disposablesForModel);
                Model.SubmitButtonEnabled.Subscribe(submitButtonForRetrieve.SetSubmittable).AddTo(_disposablesForModel);
            }
            else
            {
                Model.SubmitButtonText.SubscribeTo(submitButton).AddTo(_disposablesForModel);
                Model.SubmitButtonEnabled.Subscribe(submitButton.SetSubmittable).AddTo(_disposablesForModel);
            }
            submitGameObject.SetActive(!retrieve);
            submitGameObjectForRetrieve.SetActive(retrieve);

            Model.SubmitButtonText.SubscribeTo(submitButton).AddTo(_disposablesForModel);
            Model.SubmitButtonEnabled.Subscribe(submitButton.SetSubmittable).AddTo(_disposablesForModel);
            Model.OnSubmitClick.Subscribe(onSubmit).AddTo(_disposablesForModel);
            if (onClose != null)
            {
                Model.OnCloseClick.Subscribe(onClose).AddTo(_disposablesForModel);
            }
            Model.FooterRootActive.Subscribe(footerRoot.SetActive).AddTo(_disposablesForModel);
            // Model.itemInformation.item을 마지막으로 구독해야 위에서의 구독으로 인해 바뀌는 레이아웃 상태를 모두 반영할 수 있음.
            Model.ItemInformation.item.Subscribe(value => SubscribeTargetItem(Model.target.Value))
                .AddTo(_disposablesForModel);

            StartCoroutine(CoUpdate());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = null;
            Model.ItemInformation.item.Value = null;
            base.Close(ignoreCloseAnimation);
        }

        protected override void SubscribeTarget(RectTransform target)
        {
            // 아무 것도 하지 않도록 한다.
        }

        protected void SubscribeTargetItem(RectTransform target)
        {
            panel.SetAnchorAndPivot(AnchorPresetType.TopLeft, PivotPresetType.TopLeft);
            base.SubscribeTarget(target);

            //target과 panel이 겹칠 경우 target의 왼쪽에 다시 위치
            if (!(target is null) && panel.position.x - target.position.x < 0)
            {
                panel.SetAnchorAndPivot(AnchorPresetType.TopRight, PivotPresetType.TopRight);
                panel.MoveToRelatedPosition(target, TargetPivotPresetType.ReverseX(), DefaultOffsetFromTarget.ReverseX());
                UpdateAnchoredPosition();
            }
        }

        // NOTE: 아이템 툴팁 쪽에서 타겟의 상태를 관찰하면서 꺼주는 구조는 양방향으로 간섭이 일어나서 좋지 않아 보입니다.
        // 타겟 쪽에서 확실히 툴팁을 꺼주는 방식이 읽기 쉬울 것 같습니다.
        private IEnumerator CoUpdate()
        {
            var selectedGameObjectCache = EventSystem.current.currentSelectedGameObject;
            while (selectedGameObjectCache is null)
            {
                selectedGameObjectCache = EventSystem.current.currentSelectedGameObject;
                yield return null;
            }

            var positionCache = selectedGameObjectCache.transform.position;

            while (enabled)
            {
                var current = EventSystem.current.currentSelectedGameObject;
                if (current == selectedGameObjectCache)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        positionCache = selectedGameObjectCache.transform.position;
                        yield return null;
                        continue;
                    }

                    if (!Input.GetMouseButton(0) &&
                        Input.mouseScrollDelta == default)
                    {
                        yield return null;
                        continue;
                    }

                    var position = selectedGameObjectCache.transform.position;
                    if (position != positionCache)
                    {
                        Model.OnCloseClick.OnNext(this);
                        Close();
                        yield break;
                    }
                }
                else
                {
                    if (current == submitButton.gameObject ||
                        current == submitButtonForRetrieve.gameObject)
                    {
                        yield break;
                    }

                    Model.OnCloseClick.OnNext(this);
                    Close();
                    yield break;
                }

                yield return null;
            }
        }
    }
}
