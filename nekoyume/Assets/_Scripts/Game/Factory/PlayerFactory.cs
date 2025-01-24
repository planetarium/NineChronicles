using System;
using System.Collections.Generic;
using Libplanet.Crypto;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public static class PlayerFactory
    {
        public static GameObject Create(AvatarState avatarState)
        {
            if (avatarState is null)
            {
                throw new ArgumentNullException(nameof(avatarState));
            }

            var tableSheets = Game.instance.TableSheets;
            return Create(new Player(avatarState,
                tableSheets.CharacterSheet,
                tableSheets.CharacterLevelSheet,
                tableSheets.EquipmentItemSetEffectSheet));
        }

        public static GameObject Create(Player model = null)
        {
            var tableSheets = Game.instance.TableSheets;
            if (model is null)
            {
                model = new Player(
                    1,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet);
            }

            var objectPool = Game.instance.Stage.ObjectPool;
            var player = objectPool.Get<Character.Player>();
            if (!player)
            {
                throw new NotFoundComponentException<Character.Player>();
            }

            var address = new Address();
            player.Set(address, model, true, tableSheets);
            return player.gameObject;
        }

        public static GameObject Create(
            AvatarState avatarState,
            IEnumerable<Costume> costumes,
            Armor armor,
            Weapon weapon,
            Aura aura)
        {
            if (avatarState is null)
            {
                throw new ArgumentNullException(nameof(avatarState));
            }

            var tableSheets = Game.instance.TableSheets;
            var model = new Player(avatarState,
                tableSheets.CharacterSheet,
                tableSheets.CharacterLevelSheet,
                tableSheets.EquipmentItemSetEffectSheet);
            var objectPool = Game.instance.Stage.ObjectPool;

            var player = objectPool.Get<Character.Player>();
            if (!player)
            {
                throw new NotFoundComponentException<Character.Player>();
            }

            player.Set(avatarState.address, model, costumes, armor, weapon, aura, true, tableSheets);
            return player.gameObject;
        }
    }
}
