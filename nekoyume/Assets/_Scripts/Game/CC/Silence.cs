using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.CC
{
    public interface ISilence : ICCBase
    {
    }

    public class Silence : CCBase, ISilence
    {
        public void Set(float duration)
        {
            base.Set(duration);
        }

        protected override void OnBegin()
        {
        }

        protected override void OnTickBefore()
        {
            PopupText.Show(
                transform.TransformPoint(-0.5f, Random.Range(0.0f, 0.5f), 0.0f),
                new Vector3(-0.02f, 0.02f, 0.0f),
                "Silenced!",
                Color.blue
            );
        }
    }
}
