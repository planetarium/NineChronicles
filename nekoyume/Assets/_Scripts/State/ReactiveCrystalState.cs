using Libplanet;
using Libplanet.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Nekoyume.State
{
    public static class ReactiveCrystalState
    {
        private static readonly ReactiveProperty<FungibleAssetValue> _crystal;
        public static readonly IObservable<FungibleAssetValue> Crystal;
        public static FungibleAssetValue CrystalBalance => _crystal.HasValue ? _crystal.Value : default;

        static ReactiveCrystalState()
        {
            _crystal = new ReactiveProperty<FungibleAssetValue>();
            Crystal = _crystal.ObserveOnMainThread();
        }

        public static void Initialize(Address address)
        {
            var currency = new Currency("CRYSTAL", 18, minters: null);
            var crystal = Game.Game.instance.Agent.GetBalance(address, currency);
            _crystal.SetValueAndForceNotify(crystal);
        }

        public static void UpdateCrystal(FungibleAssetValue crystal)
        {
            if (crystal.Equals(default))
            {
                return;
            }

            _crystal.SetValueAndForceNotify(crystal);
        }
    }
}
