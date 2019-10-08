using System;
using System.Collections;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Model;
using Nekoyume.State;
using NUnit.Framework;
using UnityEngine.TestTools;
using CharacterBase = Nekoyume.Game.Character.CharacterBase;

namespace Tests
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
            _avatarState = new AvatarState(_address, agentAddress, 1, 20);
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
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.Model.Value.Targets.Add(_player.Model.Value);
            Assert.NotNull(_player);
            Assert.NotNull(((CharacterBase) _player).Model);

            foreach (var elemental in _elementalTypes)
            {
                var info = new Nekoyume.Model.Skill.SkillInfo(_player.Model.Value, 0, false, SkillCategory.Area, elemental);
                yield return _player.CoAreaAttack(new[] {info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillDoubleVFX()
        {
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.Model.Value.Targets.Add(_player.Model.Value);
            Assert.NotNull(_player);
            Assert.NotNull(((CharacterBase) _player).Model);

            foreach (ElementalType elemental in _elementalTypes)
            {
                var info = new Nekoyume.Model.Skill.SkillInfo(_player.Model.Value, 0, false, SkillCategory.Double, elemental);
                yield return _player.CoDoubleAttack(new[] {info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillBlowVFX()
        {
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.Model.Value.Targets.Add(_player.Model.Value);
            Assert.NotNull(_player);
            Assert.NotNull(((CharacterBase) _player).Model);

            foreach (ElementalType elemental in _elementalTypes)
            {
                var info = new Nekoyume.Model.Skill.SkillInfo(_player.Model.Value, 0, false, SkillCategory.Blow, elemental);
                yield return _player.CoHeal(new[] {info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillVFXWithCreate()
        {
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.Model.Value.Targets.Add(_player.Model.Value);
            Assert.NotNull(_player);
            Assert.NotNull(((CharacterBase) _player).Model);

            var pool = Game.instance.stage.objectPool;
            var objects = pool.objects["area_l_water"];
            var current = objects.Count;
            foreach (var effect in objects)
            {
                effect.SetActive(true);
            }

            var info = new Nekoyume.Model.Skill.SkillInfo(_player.Model.Value, 0, false, SkillCategory.Area,
                ElementalType.Water);
            yield return _player.CoAreaAttack(new[] {info});
            Assert.Greater(pool.objects["area_l_water"].Count, current);
        }
    }
}
