using Nekoyume.Game.Util;
using Nekoyume.Game.VFX.Skill;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    [TestFixture]
    public class SkillControllerFixture : MonoBehaviour, IMonoBehaviourTest
    {
        public bool IsTestFinished { get; private set; }
        public SkillController controller;
        public ObjectPool pool;

        public void Start()
        {
            pool = gameObject.AddComponent<ObjectPool>();
            controller = gameObject.AddComponent<SkillController>();
            IsTestFinished = true;
        }
    }
}
