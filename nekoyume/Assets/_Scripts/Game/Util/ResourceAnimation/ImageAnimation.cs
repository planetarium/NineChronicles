using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.Game.Util
{
    [RequireComponent(typeof(Image))]
    public class ImageAnimation : BaseResourceAnimation
    {
        [SerializeField] private List<Sprite> frames;
        private Image _image;

        protected override void Init()
        {
            if (!frames.Any())
            {
                NcDebug.LogError("frames list is empty. Fill in the frames.");
                return;
            }

            _image = GetComponent<Image>();
            _image.sprite = frames.First();
            _frameCount = frames.Count;
        }

        protected override void Apply(int index)
        {
            _image.sprite = frames[index];
        }
    }
}
