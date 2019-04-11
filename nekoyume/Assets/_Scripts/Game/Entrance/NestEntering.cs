using DG.Tweening;
using System.Collections;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Tween;
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
            var clearPlayers = GetComponentsInChildren<Player>(true);
            foreach (var clearPlayer in clearPlayers)
            {
                objectPool.Remove<Player>(clearPlayer.gameObject);
            }

            yield return null;
            
            for (int i = 0; i < Action.ActionManager.Instance.Avatars.Count; ++i)
            {
                var beginPos = new Vector3(-2.2f + i * 2.22f, -2.6f, 0.0f);
                var endPos = new Vector3(-2.2f + i * 2.22f, -0.88f, 0.0f);
                var placeRes = Resources.Load<GameObject>("Prefab/PlayerPlace");
                if (i % 2 == 0)
                    endPos.y = -1.1f;
                var avatar = Action.ActionManager.Instance.Avatars[i];

                var factory = GetComponent<PlayerFactory>();
                if (ReferenceEquals(factory, null))
                {
                    throw new NotFoundComponentException<PlayerFactory>();
                }

                GameObject go;
                bool active;
                if (avatar != null)
                {
                    go = factory.Create(avatar);
                    var anim = go.GetComponentInChildren<Animator>();
                    anim.Play("Appear");
                    active = true;
                }
                else
                {
                    go = factory.Create();
                    active = false;
                }
                go.transform.position = beginPos;
                var place = Instantiate(placeRes, go.transform);

                // player animator
                go.transform.GetChild(0).gameObject.SetActive(active);

                var tween = place.GetComponentInChildren<DOTweenSpriteAlpha>();
                tween.gameObject.SetActive(active);

                go.transform.DOMove(endPos, 2.0f).SetEase(Ease.OutBack);
                yield return new WaitForSeconds(0.2f);
            }

            ActionCamera.instance.SetPoint(0f, 0f);

            yield return new WaitForSeconds(1.0f);

            UI.Widget.Find<UI.Login>().ready = true;

            Destroy(this);
        }
    }
}
