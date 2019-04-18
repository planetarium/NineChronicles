using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;


namespace Nekoyume.UI
{
    public class Blind : Widget
    {
        [SerializeField]
        private Image _image;
        [SerializeField]
        private Text _content;

        public IEnumerator FadeIn(float time, string text = "")
        {
            Show();
            _image.DOFade(0.0f, 0.0f);
            _image.DOFade(1.0f, time);
            yield return new WaitForSeconds(time);
            _content.text = text;
        }

        public IEnumerator FadeOut(float time)
        {
            _content.text = "";
            _image.DOFade(0.0f, time);
            yield return new WaitForSeconds(time);
            Close();
        }
    }
}
