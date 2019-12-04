using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class BulletedStatView : StatView
    {
        public Image bulletMainImage;
        public Image bulletSubImage;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public bool IsShow => gameObject.activeSelf;
        public Model.BulletedStatView Model { get; private set; }

        public void Show(Model.BulletedStatView model)
        {
            if (model is null)
            {
                Hide();
                
                return;
            }
            
            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.IsMainStat.Subscribe(isMainStat =>
            {
                bulletMainImage.enabled = isMainStat;
                bulletSubImage.enabled = !isMainStat;
            }).AddTo(_disposablesForModel);
            Model.Key.SubscribeTo(statTypeText).AddTo(_disposablesForModel);
            Model.Value.SubscribeTo(valueText).AddTo(_disposablesForModel);
            Show();
        }

        public override void Show()
        {
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
            Model?.Dispose();
            Model = null;
            _disposablesForModel.DisposeAllAndClear();
        }
    }
}
