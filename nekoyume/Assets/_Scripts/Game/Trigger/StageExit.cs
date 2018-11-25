using System.Collections.Generic;
using DG.Tweening;
using Nekoyume.Game.Character;
using Nekoyume.Move;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class StageExit : MonoBehaviour
    {
        public void SetEnable()
        {
            Collider2D collider = GetComponent<Collider2D>();
            collider.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.name == "Player")
            {
                Collider2D collider = GetComponent<Collider2D>();
                collider.enabled = false;
                var player = other.gameObject.GetComponent<Player>();
                var stage = GetComponentInParent<Stage>();
                var id = 1;
                if (stage.Id < 3) id = stage.Id + 1;
                MoveManager.Instance.HackAndSlash(player, id);
                stage.OnStageEnter();
            }
        }
    }
}
