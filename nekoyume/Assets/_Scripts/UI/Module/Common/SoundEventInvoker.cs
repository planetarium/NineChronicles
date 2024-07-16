using Nekoyume.Game.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module.Common
{
    public class SoundEventInvoker : MonoBehaviour
    {
        public void PlaySfx(string sfx)
        {
            AudioController.instance.PlaySfx(sfx);
        }

        public enum sfx
        {
            sfx_star
        }

        public void PlaySfxByEnum(sfx sfx)
        {
            AudioController.instance.PlaySfx(sfx.ToString());
        }
    }
}
