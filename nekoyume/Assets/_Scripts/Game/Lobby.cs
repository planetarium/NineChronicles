using Nekoyume.Game.Character;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Lobby : MonoBehaviour
    {
        [SerializeField]
        private LobbyCharacter character;

        public LobbyCharacter Character => character;
    }
}
