using DG.Tweening;
using System.Collections;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class NestEntering : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var stage = GetComponent<Stage>();
            stage.LoadBackground("nest");

            UI.Widget.Find<UI.Login>().ready = false;

            var objectPool = GetComponent<Util.ObjectPool>();
            var clearPlayers = GetComponentsInChildren<Character.Player>(true);
            foreach (var clearPlayer in clearPlayers)
            {
                objectPool.Remove<Character.Player>(clearPlayer.gameObject);
            }

            yield return null;
            
            for (int i = 0; i < Action.ActionManager.Instance.Avatars.Count; ++i)
            {
                var beginPos = new Vector3(-2.0f + i * 2.22f, -2.0f, 0.0f);
                var endPos = new Vector3(-2.0f + i * 2.22f, -0.4f, 0.0f);
                var placeRes = Resources.Load<GameObject>("Prefab/PlayerPlace");
                if (i % 2 == 0)
                    endPos.y = -0.66f;
                var avatar = Action.ActionManager.Instance.Avatars[i];
                var player = objectPool.Get<Character.Player>();
                player.transform.position = beginPos;
                Instantiate(placeRes, player.transform);
                player.transform.DOMove(endPos, 2.0f).SetEase(Ease.OutBack);
                var anim = player.GetComponentInChildren<Animator>();
                anim.Play("Appear");
                if (avatar != null)
                {
                    player.Init(avatar.ToPlayer());
                }
                else
                {
                    player.transform.Find("Animator")?.gameObject.SetActive(false);
                    var tween = player.GetComponentInChildren<Tween.DOTweenSpriteAlpha>();
                    tween.gameObject.SetActive(false);
                }
                yield return new WaitForSeconds(0.2f);
            }

            var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
            var camPos = cam.transform.position;
            camPos.x = 0.0f;
            camPos.y = 0.0f;
            cam.transform.position = camPos;

            yield return new WaitForSeconds(1.0f);

            UI.Widget.Find<UI.Login>().ready = true;

            Destroy(this);
        }
    }
}
