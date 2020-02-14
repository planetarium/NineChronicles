using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CategoryButton : MonoBehaviour, IToggleable
    {
        public Button button;
        public Image effectImage;
        public Animator animator;
        
        private IToggleListener _toggleListener;

        protected void Awake()
        {
            IsToggleable = true;

            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                animator?.Play("SubmitSelected");
                _toggleListener?.OnToggle(this);
            }).AddTo(gameObject);
        }
        
        #region IToggleable

        public string Name => name;
        public bool IsToggleable { get; set; }
        public bool IsToggledOn => effectImage.enabled;

        public void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        public void SetToggledOn()
        {
            button.interactable = false;
            effectImage.enabled = true;
        }
        
        public void SetToggledOff()
        {
            button.interactable = true;
            effectImage.enabled = false;
        }

        #endregion
    }
}
