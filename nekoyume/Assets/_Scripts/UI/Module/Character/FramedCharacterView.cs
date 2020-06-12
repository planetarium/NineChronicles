using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class FramedCharacterView : VanillaCharacterView
    {
        [SerializeField]
        private Image frameImage = null;

        protected override void SetDim(bool isDim)
        {
            var alpha = isDim ? .3f : 1f;
            frameImage.color = GetColor(frameImage.color, alpha);
        }
    }
}
