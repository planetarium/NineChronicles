using System;
using System.Collections;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Factory;
using Nekoyume.Model;
using Nekoyume.State;
using NUnit.Framework;
using UnityEngine.TestTools;
using CharacterBase = Nekoyume.Game.Character.CharacterBase;

namespace Tests.PlayMode
{
    public class SkillControllerTest
    {
        private Nekoyume.Game.Character.Player _player;
        private readonly ElementalType[] _elementalTypes;
        private Address _address;
        private AvatarState _avatarState;

        public SkillControllerTest()
        {
            _elementalTypes = (ElementalType[])
                Enum.GetValues(typeof(ElementalType));
        }

        [SetUp]
        public void Setup()
        {
            _address = new Address();
            var agentAddress = new Address();
            _avatarState = new AvatarState(_address, agentAddress, 1, Game.instance.TableSheets.WorldSheet,
                Game.instance.TableSheets.QuestSheet);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.Destroy(_player);
        }

        [UnityTest]
        public IEnumerator GetSkillAreaVFX()
        {
            //FIXME Setup이나 Constructor에서 플레이어를 설정하면 파괴되는 MonoSingleton의 상태가 꼬이는 문제가 있음.
            var go = PlayerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.Model.Targets.Add(_player.Model);
            Assert.NotNull(_player);
            Assert.NotNull(_player.CharacterModel);

            foreach (var elemental in _elementalTypes)
            {
                var info = new Nekoyume.Model.Skill.SkillInfo(_player.CharacterModel, 0, false, SkillCategory.AreaAttack, 0, elemental);
                yield return _player.CoAreaAttack(new[] {info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillDoubleVFX()
        {
            var go = PlayerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.CharacterModel.Targets.Add(_player.CharacterModel);
            Assert.NotNull(_player);
            Assert.NotNull(_player.CharacterModel);

            foreach (ElementalType elemental in _elementalTypes)
            {
                var info = new Nekoyume.Model.Skill.SkillInfo(_player.CharacterModel, 0, false, SkillCategory.DoubleAttack, 0, elemental);
                yield return _player.CoDoubleAttack(new[] {info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillBlowVFX()
        {
            var go = PlayerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.CharacterModel.Targets.Add(_player.CharacterModel);
            Assert.NotNull(_player);
            Assert.NotNull(((CharacterBase) _player).CharacterModel);

            foreach (ElementalType elemental in _elementalTypes)
            {
                var info = new Nekoyume.Model.Skill.SkillInfo(_player.CharacterModel, 0, false, SkillCategory.BlowAttack, 0, elemental);
                yield return _player.CoHeal(new[] {info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillVFXWithCreate()
        {
            var go = PlayerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.CharacterModel.Targets.Add(_player.CharacterModel);
            Assert.NotNull(_player);
            Assert.NotNull(((CharacterBase) _player).CharacterModel);

            var pool = Game.instance.Stage.objectPool;
            var objects = pool.objects["area_l_water"];
            var current = objects.Count;
            foreach (var effect in objects)
            {
                effect.SetActive(true);
            }

            var info = new Nekoyume.Model.Skill.SkillInfo(_player.CharacterModel, 0, false, SkillCategory.AreaAttack,
                0, ElementalType.Water);
            yield return _player.CoAreaAttack(new[] {info});
            Assert.Greater(pool.objects["area_l_water"].Count, current);
        }
    }
}
