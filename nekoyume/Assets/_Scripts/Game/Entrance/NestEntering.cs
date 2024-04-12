using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Blockchain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Tween;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class NestEntering : MonoBehaviour
    {
        private IEnumerator Start()
        {
            DOTween.KillAll();
            var stage = Game.instance.Stage;
            stage.LoadBackground("nest");

            Widget.Find<Login>().ready = false;
            yield return null;

            Game.instance.SavedPetId = null;

            var players = new List<Player>();
            for (var i = 0; i < GameConfig.SlotCount; i++)
            {
                Player player;
                bool active;
                var beginPos = stage.SelectPositionBegin(i);
                var endPos = stage.SelectPositionEnd(i);
                var placeRes = Resources.Load<GameObject>("Prefab/PlayerPlace");
                if (i % 2 == 0)
                    endPos.y = -0.45f;
                if (States.Instance.AvatarStates.TryGetValue(i, out var avatarState))
                {
                    var itemSlotState = States.Instance.ItemSlotStates[i][BattleType.Adventure];
                    var costumeInventory = avatarState.inventory.Costumes;
                    var costumes = itemSlotState.Costumes
                        .Select(guid => costumeInventory.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();

                    var equipmentInventory = avatarState.inventory.Equipments;
                    var equipments = itemSlotState.Equipments
                        .Select(guid => equipmentInventory.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();

                    var armor = equipments.OfType<Armor>().FirstOrDefault();
                    var weapon = equipments.OfType<Weapon>().FirstOrDefault();
                    var aura = equipments.OfType<Aura>().FirstOrDefault();
                    player = PlayerFactory.Create(avatarState, costumes, armor, weapon, aura).GetComponent<Player>();
                    player.SpineController.Appear();
                    active = true;
                }
                else
                {
                    player = PlayerFactory.Create().GetComponent<Player>();
                    // player.SpineController.Hide();
                    active = false;
                }

                var playerTransform = player.transform;
                playerTransform.position = beginPos;
                var place = Instantiate(placeRes, playerTransform);

                // player animator
                if (player.Animator.Target is not null)
                {
                    player.Animator.Target.SetActive(active);
                }

                var tween = place.GetComponentInChildren<DOTweenSpriteAlpha>();
                tween.gameObject.SetActive(active);

                playerTransform.DOMove(endPos, 2.0f).SetEase(Ease.OutBack);

                var seqPos = new Vector3(endPos.x, endPos.y - Random.Range(0.05f, 0.1f), 0.0f);
                var seq = DOTween.Sequence();
                seq.Append(playerTransform.DOMove(seqPos, Random.Range(4.0f, 5.0f)));
                seq.Append(playerTransform.DOMove(endPos, Random.Range(4.0f, 5.0f)));
                seq.Play().SetDelay(2.0f).SetLoops(-1);
                players.Add(player);

                yield return new WaitForSeconds(0.2f);
            }

            ActionCamera.instance.SetPosition(0f, 0f);

            yield return new WaitForSeconds(1.0f);

            Widget.Find<Login>().ready = true;
            Widget.Find<Login>().players = players;

            Destroy(this);
        }
    }
}
