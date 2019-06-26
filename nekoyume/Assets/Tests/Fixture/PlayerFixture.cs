using Libplanet;
using Nekoyume.Game.Character;
using Nekoyume.State;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    [TestFixture]
    public class PlayerFixture : MonoBehaviour, IMonoBehaviourTest
    {
        public bool IsTestFinished { get; private set; }
        public Player player;

        public void Start()
        {
            var address = new Address();
            var state = new AvatarState(address);
            player = gameObject.AddComponent<Player>();
            var model = new Nekoyume.Model.Player(state);
            player.Init(model);
            IsTestFinished = true;
        }

    }
}
