using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimation : BaseResourceAnimation
    {
        [SerializeField] private List<Sprite> frames;
        private SpriteRenderer _spriteRenderer;

        protected override void Init()
        {
            if (!frames.Any())
            {
                NcDebug.LogError("frames list is empty. Fill in the frames.");
                return;
            }

            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = frames.First();
            _frameCount = frames.Count;
        }

        protected override void Apply(int index)
        {
            _spriteRenderer.sprite = frames[index];
        }
    }
}
