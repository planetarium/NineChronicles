using Nekoyume.Game.Character;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Lobby : MonoBehaviour
    {
        [SerializeField]
        private LobbyCharacter character;

        [SerializeField]
        private FriendCharacter friendCharacter;

        // [SerializeField]
        // private Avatar.Avatar avatar;

        public LobbyCharacter Character => character;
        public FriendCharacter FriendCharacter => friendCharacter;
        // public Avatar.Avatar Avatar => avatar;
    }
}
