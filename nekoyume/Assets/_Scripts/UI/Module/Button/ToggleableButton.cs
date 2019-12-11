using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ToggleableButton : MonoBehaviour, IToggleable
    {
        [SerializeField] public Button button;
        [SerializeField] public Image toggledOffImage;
        [SerializeField] private Image toggledOnImage;

        private IToggleListener _toggleListener;
        
        public string Name => name;
        public bool IsToggledOn => toggledOnImage.gameObject.activeSelf;

        private void Awake()
        {
            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    _toggleListener?.OnToggle(this);
                })
                .AddTo(gameObject);
        }

        public void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        public void SetToggledOn()
        {
            toggledOffImage.gameObject.SetActive(false);
            toggledOnImage.gameObject.SetActive(true);
        }

        public void SetToggledOff()
        {
            toggledOffImage.gameObject.SetActive(true);
            toggledOnImage.gameObject.SetActive(false);
        }
    }
}
