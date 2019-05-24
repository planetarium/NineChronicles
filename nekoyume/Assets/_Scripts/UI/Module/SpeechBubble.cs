using Assets.SimpleLocalization;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI.Module
{
    public class SpeechBubble : MonoBehaviour
    {
        public string localizationKey;
        public Image bubbleImage;
        public Text text;
        public string imageName;
        public Vector3 positionOffset;

        private int _speechCount = 0;

        #region Mono

        protected virtual void Awake()
        {
            this.ComponentFieldsNotNullTest();
        }

        protected virtual void OnDestroy()
        {
        }

        #endregion

        public void Init()
        {
            _speechCount = LocalizationManager.LocalizedCount(localizationKey);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            if (_speechCount == 0)
                return;

            Menu parent = GetComponentInParent<Menu>();
            if (!parent)
                throw new NotFoundComponentException<Menu>();

            var rect = bubbleImage.GetComponent<RectTransform>();
            var targetImage = parent.Stage.background.transform.Find(imageName);
            if (!targetImage)
                return;

            var position = targetImage.position + positionOffset;
            rect.anchoredPosition = position.ToCanvasPosition(Game.ActionCamera.instance.Cam, MainCanvas.instance.Canvas);

            text.text = LocalizationManager.Localize($"{localizationKey}{Random.Range(0, _speechCount)}");

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Update()
        {
            Menu parent = GetComponentInParent<Menu>();
            if (!parent)
                throw new NotFoundComponentException<Menu>();

            var rect = bubbleImage.GetComponent<RectTransform>();
            var targetImage = parent.Stage.background.transform.Find(imageName);
            if (!targetImage)
                return;

            var position = targetImage.position + positionOffset;
            rect.anchoredPosition = position.ToCanvasPosition(Game.ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
        }
    }
}
