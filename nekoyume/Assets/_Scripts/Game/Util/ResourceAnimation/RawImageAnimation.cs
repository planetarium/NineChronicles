using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.Game.Util
{
    [RequireComponent(typeof(RawImage))]
    public class RawImageAnimation : BaseResourceAnimation
    {
        [SerializeField] private List<Texture> frames;
        private RawImage _rawImage;

        protected override void Init()
        {
            if (!frames.Any())
            {
                NcDebug.LogError("frames list is empty. Fill in the frames.");
                return;
            }

            _rawImage = GetComponent<RawImage>();
            _rawImage.texture = frames.First();
            _frameCount = frames.Count;
        }

        protected override void Apply(int index)
        {
            _rawImage.texture = frames[index];
        }
    }
}
