using Libplanet;
using Libplanet.Assets;
using Nekoyume.Helper;
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
            var crystal = Game.Game.instance.Agent.GetBalance(address, CrystalCalculator.CRYSTAL);
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

        public static void AddCrystal(FungibleAssetValue crystal) => UpdateCrystal(_crystal.Value + crystal);
    }
}
