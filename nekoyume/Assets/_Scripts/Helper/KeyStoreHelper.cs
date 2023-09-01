using System;
using System.IO;
using System.Linq;
using System.Text;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using UnityEditor;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class KeyStoreHelper
    {
        public static IKeyStore GetKeystore()
        {
            IKeyStore store;

            if (Platform.IsMobilePlatform())
            {
                string dataPath = Platform.GetPersistentDataPath("keystore");
                store = new Web3KeyStore(dataPath);
            }
            else
            {
                store = Web3KeyStore.DefaultKeyStore;
            }

            return store;
        }

        public static void ResetPassword(Address addressToReset, string originPassword, string newPassword)
        {
            var store = GetKeystore();
            var tuple = store.List().FirstOrDefault(tuple => tuple.Item2.Address == addressToReset);
            if (tuple is null)
            {
                Debug.Log($"Keystore not has key. Address: {addressToReset}");
                return;
            }

            PrivateKey pk;
            try
            {
                pk = tuple.Item2.Unprotect(originPassword);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            ResetPassword(pk, newPassword);
        }

        public static void ResetPassword(PrivateKey pk, string newPassword)
        {
            var store = GetKeystore();
            var tuple = store.List().FirstOrDefault(tuple => tuple.Item2.Address == pk.PublicKey.ToAddress());
            if (tuple is null)
            {
                Debug.Log($"Keystore not has key. Address: {pk.PublicKey.ToAddress()}");
            }
            else
            {
                store.Remove(tuple.Item1);
            }

            store.Add(ProtectedPrivateKey.Protect(pk, newPassword));
            Debug.Log($"{pk.PublicKey.ToAddress()} password reset success!");
        }
    }
}
