using System;
using System.Numerics;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarCrystalModifier : AvatarStateModifier
    {
        [SerializeField]
        private FungibleAssetValue crystal;

        public override bool IsEmpty => crystal.Sign == 0;

        public AvatarCrystalModifier(FungibleAssetValue crystal)
        {
            this.crystal = crystal;
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarCrystalModifier m))
            {
                return;
            }

            crystal += m.crystal;
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarCrystalModifier m))
            {
                return;
            }

            crystal -= m.crystal;
        }

        public override AvatarState Modify(AvatarState state)
        {
            return state;
        }

        public override string ToString()
        {
            return $"{nameof(crystal)}: {crystal.MajorUnit}";
        }
    }
}
