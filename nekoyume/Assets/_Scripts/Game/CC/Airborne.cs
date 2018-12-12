using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.CC
{
    public interface IAirborne : IStun
    {
    }

    public class Airborne : Stun, IAirborne
    {
        protected override void OnTickBefore()
        {
            PopupText.Show(
                transform.TransformPoint(-0.5f, Random.Range(0.0f, 0.5f), 0.0f),
                new Vector3(-0.02f, 0.02f, 0.0f),
                "Airborned!",
                Color.white
            );
        }
    }
}
