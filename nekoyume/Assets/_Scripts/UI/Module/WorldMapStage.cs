using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI.Module
{
    public class WorldMapStage : MonoBehaviour
    {
        [Serializable]
        public struct SpriteSet
        {
            public Sprite normal;
            public Sprite selected;
            public Sprite cleared;
            public Sprite disabled;
        }

        public SpriteSet spriteSet;
        public float normalScale = 0.45f;
        public float bossScale = 0.6f;
        
        public Image bossImage;
        public Image stageImage;
        public Button button;
        public Text buttonText;

        public Tween.DOTweenRectTransformMoveBy tweenMove;
        public Tween.DOTweenGroupAlpha tweenAlpha;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();
        
        public Model.WorldMapStage Model { get; private set; }

        private void Awake()
        {
            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Model.onClick.OnNext(this);
                }).AddTo(gameObject);
        }

        public void SetModel(Model.WorldMapStage model)
        {
            if (model == null)
            {
                Clear();
                
                return;
            }
            
            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.state.Subscribe(Subscribe).AddTo(_disposablesForModel);
            Model.hasBoss.Subscribe(SetBoss).AddTo(_disposablesForModel);
            Model.stage.SubscribeToText(buttonText).AddTo(_disposablesForModel);
        }

        public void Clear()
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = null;
        }

        private void Subscribe(Model.WorldMapStage.State value)
        {
            switch (value)
            {
                case UI.Model.WorldMapStage.State.Normal:
                    stageImage.sprite = spriteSet.normal;
                    button.enabled = true;
                    break;
                case UI.Model.WorldMapStage.State.Selected:
                    stageImage.sprite = spriteSet.selected;
                    button.enabled = true;
                    break;
                case UI.Model.WorldMapStage.State.Cleared:
                    stageImage.sprite = spriteSet.cleared;
                    button.enabled = true;
                    break;
                case UI.Model.WorldMapStage.State.Disabled:
                    stageImage.sprite = spriteSet.disabled;
                    button.enabled = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
            
            stageImage.SetNativeSize();
        }

        private void SetBoss(bool isBoss)
        {
            bossImage.enabled = isBoss;
            stageImage.transform.localScale = isBoss
                ? new float3(bossScale, bossScale, 1f)
                : new float3(normalScale, normalScale, 1f);
        }
    }
}
