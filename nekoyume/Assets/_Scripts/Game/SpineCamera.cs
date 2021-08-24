using Nekoyume.Pattern;
using UnityEngine;

namespace Nekoyume.Game
{
    [RequireComponent(typeof(Camera))]
    public class SpineCamera : MonoSingleton<SpineCamera>
    {
        protected override void Awake()
        {
            base.Awake();
            transform.localPosition = new Vector3(1000.0f, 1000.0f, -100.0f);
        }
    }
}
