using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;


namespace Nekoyume.UI
{
    public class Blind : Widget
    {
        public Image image;
        public Text content;

        protected override void Awake()
        {
        }

        public IEnumerator FadeIn(float time, string text = "", float fadeValue = 1.0f)
        {
            Show();
            image.DOFade(0.0f, 0.0f);
            image.DOFade(fadeValue, time);
            yield return new WaitForSeconds(time);
            content.text = text;
        }

        public IEnumerator FadeOut(float time)
        {
            content.text = "";
            image.DOFade(0.0f, time);
            yield return new WaitForSeconds(time);
            Close();
        }
    }
}
