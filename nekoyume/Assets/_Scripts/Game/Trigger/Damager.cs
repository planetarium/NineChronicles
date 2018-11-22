using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class Damager : MonoBehaviour
    {
        private Util.SpriteAnimator _anim;

        public void Set(string targetTag, int damage, float size, int targetCount)
        {
            var anim = GetComponent<Util.SpriteAnimator>();
            anim.Play("hit_01");

            var stage = gameObject.GetComponentInParent<Stage>();
            Character.Base[] characters = stage.GetComponentsInChildren<Character.Base>();
            foreach (var character in characters)
            {
                if (targetCount <= 0)
                    break;

                Debug.DrawLine(transform.position, transform.TransformPoint(size, 0.0f, 0.0f), Color.green, 1.0f);
                if (transform.position.x > character.transform.position.x
                    || transform.position.x + size < character.transform.position.x)
                    continue;

                // TODO: Use tag
                if (character.name == targetTag)
                {
                    character.HP -= damage;
                    if (character.HP < 0)
                    {
                        character.gameObject.SetActive(false);
                    }
                }
                --targetCount;
            }
        }
    }
}
