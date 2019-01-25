using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class EventBase
    {
        public CharacterBase character;
        public CharacterBase target;
        public Guid characterId;
        public Guid targetId;

        public abstract void Execute(Stage stage);
    }
}
