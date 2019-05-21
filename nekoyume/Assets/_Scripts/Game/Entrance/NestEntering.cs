using DG.Tweening;
using System.Collections;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Tween;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class NestEntering : MonoBehaviour
    {
        private Stage _stage;

        private IEnumerator Start()
        {
            _stage = GetComponent<Stage>();
            _stage.LoadBackground("nest");

            UI.Widget.Find<UI.Login>().ready = false;

            var objectPool = GetComponent<Util.ObjectPool>();
            var clearPlayers = GetComponentsInChildren<Player>(true);
            foreach (var clearPlayer in clearPlayers)
            {
                objectPool.Remove<Player>(clearPlayer.gameObject);
            }

            _stage.selectedPlayer = null;

            yield return null;

            var avatars = Agent.Avatars;
            for (int i = 0; i < avatars.Count; ++i)
            {
                var beginPos = new Vector3(-2.2f + i * 2.22f, -2.6f, 0.0f);
                var endPos = new Vector3(-2.2f + i * 2.22f, -0.88f, 0.0f);
                var placeRes = Resources.Load<GameObject>("Prefab/PlayerPlace");
                if (i % 2 == 0)
                    endPos.y = -1.1f;
                var avatar = avatars[i];

                var factory = GetComponent<PlayerFactory>();
                if (ReferenceEquals(factory, null))
                {
                    throw new NotFoundComponentException<PlayerFactory>();
                }

                Player player;
                bool active;
                if (avatar != null)
                {
                    player = factory.Create(avatar).GetComponent<Player>();
                    player.animator.Appear();
                    active = true;
                }
                else
                {
                    player = factory.Create().GetComponent<Player>();
                    active = false;
                }
                player.transform.position = beginPos;
                var place = Instantiate(placeRes, player.transform);

                // player animator
                player.animator.Target.SetActive(active);

                var tween = place.GetComponentInChildren<DOTweenSpriteAlpha>();
                tween.gameObject.SetActive(active);

                player.transform.DOMove(endPos, 2.0f).SetEase(Ease.OutBack);
                yield return new WaitForSeconds(0.2f);
            }

            ActionCamera.instance.SetPoint(0f, 0f);

            yield return new WaitForSeconds(1.0f);

            UI.Widget.Find<UI.Login>().ready = true;

            Destroy(this);
        }
    }
}
