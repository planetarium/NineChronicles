using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Libplanet;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class WeeklyArenaInfoActivator : WeeklyArenaStateModifier
    {
        [SerializeField]
        private List<string> avatarAddressHexList;

        public override bool IsEmpty => avatarAddressHexList.Count == 0;

        public WeeklyArenaInfoActivator(Address avatarAddress)
        {
            var addressHex = avatarAddress.ToHex();
            Debug.LogWarning(addressHex);
            avatarAddressHexList = new List<string>();

            if (addressHex.Equals(default(Address).ToHex()))
                return;
            
            avatarAddressHexList.Add(addressHex);
        }
        
        public override void Add(IAccumulatableStateModifier<WeeklyArenaState> modifier)
        {
            if (!(modifier is WeeklyArenaInfoActivator m))
                return;

            foreach (var incoming in m.avatarAddressHexList.Where(incoming => !avatarAddressHexList.Contains(incoming)))
            {
                avatarAddressHexList.Add(incoming);
            }
        }

        public override void Remove(IAccumulatableStateModifier<WeeklyArenaState> modifier)
        {
            if (!(modifier is WeeklyArenaInfoActivator m))
                return;

            foreach (var incoming in m.avatarAddressHexList.Where(incoming => avatarAddressHexList.Contains(incoming)))
            {
                avatarAddressHexList.Remove(incoming);
            }
        }

        public override WeeklyArenaState Modify(WeeklyArenaState state)
        {
            if (state is null)
                throw new ArgumentNullException(nameof(state));

            foreach (var addressHex in avatarAddressHexList)
            {
                var address = new Address(addressHex);
                if (state.TryGetValue(address, out var arenaInfo))
                {
                    arenaInfo.Activate();    
                }
                else
                {
                    var sb = new StringBuilder($"[{nameof(WeeklyArenaInfoActivator)}]");
                    sb.Append($"{nameof(Modify)} Not found `{nameof(ArenaInfo)}` exception.");
                    sb.Append($"{nameof(addressHex)}: {addressHex}");
                    Debug.LogWarning(sb.ToString());
                }
            }

            return state;
        }
    }
}
