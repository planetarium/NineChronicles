using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    [RequireComponent(typeof(Image))]
    public class FlowingImage : FlowingRectTransform
    {
        public Image image;

        protected override void Reset()
        {
            base.Reset();
            image = GetComponent<Image>();
        }
    }
}
