using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;


namespace Nekoyume.UI
{
    public class Blind : Popup
    {
        public Image image;
        public Text content;
        public void Show(string text)
        {
            content.text = text;
            image.DOFade(0.0f, 0.0f);
            base.Show();
        }

        public void FadeIn(float time)
        {
            image.DOFade(1.0f, time);
        }

        public void FadeOut(float time)
        {
            image.DOFade(0.0f, time);
        }
    }
}
