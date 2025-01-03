using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.TableData.AdventureBoss;
using UnityEngine;

namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        public Vector3[] spawnPoints;

        private Model.Enemy _enemy;

        private int _wave;
        private const float SpawnOffset = 6.0f;

        private class InvalidWaveException : Exception
        {
        }

        public IEnumerator CoSetData(List<Model.Enemy> monsters)
        {
            yield return StartCoroutine(CoSpawnWave(monsters));
        }

        private IEnumerator CoSpawnWave(List<Model.Enemy> monsters)
        {
            var stage = Game.instance.Stage;
            for (var index = 0; index < monsters.Count; index++)
            {
                var monster = monsters[index];
                monster.spawnIndex = index;

                var player = stage.SelectedPlayer;
                var offsetX = player.transform.position.x + SpawnOffset;
                {
                    Vector3 point;
                    try
                    {
                        point = spawnPoints[index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidWaveException();
                    }

                    var pos = new Vector2(
                        point.x + offsetX,
                        point.y);
                    yield return StartCoroutine(CoSpawnMonster(monster, pos, player));
                }
            }
        }

        private IEnumerator CoSpawnMonster(Model.Enemy enemy, Vector2 pos, Player player)
        {
            StageMonsterFactory.Create(enemy, pos, player);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.0f, 0.2f));
        }

        private IEnumerator CoSpawnBreakthroughMonster(int enemyId, Vector2 pos, float offset, Player player, Action<GameObject> createCallback)
        {
            var createdObj = StageMonsterFactory.CreateBreakthroughCharacter(enemyId, pos, offset, player);
            createCallback.Invoke(createdObj);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.0f, 0.2f));
        }

        public IEnumerator CoSpawnEnemyPlayer(Model.EnemyPlayer enemy, Vector3 offset)
        {
            var enemyPlayer = StageMonsterFactory.Create(enemy, offset);
            enemyPlayer.StartRun();
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.0f, 0.2f));
        }

        public IEnumerator CoSpawnWave(List<int> monsterIds, Vector2 position, float offset, PrologueCharacter fenrir, Player player)
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.FenrirGrowlSummon);
            for (var index = 0; index < monsterIds.Count; index++)
            {
                fenrir.Animator.Cast();
                var id = monsterIds[index];
                var pos = new Vector2(
                    spawnPoints[index].x + position.x + offset,
                    spawnPoints[index].y);
                var go = StageMonsterFactory.Create(id, pos, offset, player, true);
                var enemy = go.GetComponent<PrologueCharacter>();
                yield return new WaitUntil(() => enemy.Animator.IsIdle());
                yield return new WaitForSeconds(1f);
                fenrir.Animator.Idle();
                yield return new WaitForSeconds(0.3f);
            }
        }

        public IEnumerator CoSpawnBreakthrough(List<AdventureBossFloorWaveSheet.MonsterData> monsterDatas, Action<GameObject> createCallback)
        {
            var stage = Game.instance.Stage;
            for (var index = 0; index < monsterDatas.Count; index++)
            {
                var player = stage.SelectedPlayer;
                var offset = 6;
                var offsetX = player.transform.position.x + offset;
                {
                    Vector3 point;
                    try
                    {
                        point = spawnPoints[index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidWaveException();
                    }

                    var pos = new Vector2(
                        offsetX + point.x,
                        point.y);
                    yield return StartCoroutine(CoSpawnBreakthroughMonster(monsterDatas[index].CharacterId, pos, 0, player, createCallback));
                }
            }
        }
    }
}
