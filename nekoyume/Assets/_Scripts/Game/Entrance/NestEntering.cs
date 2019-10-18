using DG.Tweening;
using System.Collections;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Tween;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class NestEntering : MonoBehaviour
    {
        private IEnumerator Start()
        {
            DOTween.KillAll();
            var stage = Game.instance.stage;
            stage.LoadBackground("nest");

            Widget.Find<Login>().ready = false;

            var objectPool = GetComponent<Util.ObjectPool>();
            var clearPlayers = GetComponentsInChildren<Player>(true);
            foreach (var clearPlayer in clearPlayers)
            {
                clearPlayer.DisableHUD();
                objectPool.Remove<Player>(clearPlayer.gameObject);
            }

            stage.selectedPlayer = null;
            yield return null;

            var factory = GetComponent<PlayerFactory>();
            if (ReferenceEquals(factory, null))
            {
                throw new NotFoundComponentException<PlayerFactory>();
            }

            for (var i = 0; i < GameConfig.SlotCount; i++)
            {
                Player player;
                bool active;
                var beginPos = stage.selectPositionBegin(i);
                var endPos = stage.selectPositionEnd(i);
                var placeRes = Resources.Load<GameObject>("Prefab/PlayerPlace");
                if (i % 2 == 0)
                    endPos.y = -1.1f;
                if (States.Instance.AvatarStates.TryGetValue(i, out var avatarState))
                {
                    player = factory.Create(avatarState).GetComponent<Player>();
                    player.Animator.Appear();
                    active = true;
                }
                else
                {
                    player = factory.Create().GetComponent<Player>();
                    active = false;
                }

                var playerTransform = player.transform;
                playerTransform.position = beginPos;
                var place = Instantiate(placeRes, playerTransform);

                // player animator
                player.Animator.Target.SetActive(active);

                var tween = place.GetComponentInChildren<DOTweenSpriteAlpha>();
                tween.gameObject.SetActive(active);

                playerTransform.DOMove(endPos, 2.0f).SetEase(Ease.OutBack);

                var seqPos = new Vector3(endPos.x, endPos.y - Random.Range(0.05f, 0.1f), 0.0f);
                var seq = DOTween.Sequence();
                seq.Append(playerTransform.DOMove(seqPos, Random.Range(4.0f, 5.0f)));
                seq.Append(playerTransform.DOMove(endPos, Random.Range(4.0f, 5.0f)));
                seq.Play().SetDelay(2.0f).SetLoops(-1);

                yield return new WaitForSeconds(0.2f);
            }

            ActionCamera.instance.SetPoint(0f, 0f);

            yield return new WaitForSeconds(1.0f);

            Widget.Find<Login>().ready = true;

            Destroy(this);
        }
    }
}
