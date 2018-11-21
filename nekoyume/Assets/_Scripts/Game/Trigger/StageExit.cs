using DG.Tweening;
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

                var stage = GetComponentInParent<Stage>();
                stage.OnStageEnter();
            }
        }
    }
}
