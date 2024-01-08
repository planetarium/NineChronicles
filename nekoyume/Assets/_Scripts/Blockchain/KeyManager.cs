using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using UnityEngine;

namespace Nekoyume.Blockchain
{
    /// <summary>
    /// Manage a single key store.
    /// Use <see cref="Instance"/> to access the singleton instance.
    /// Initialize the key store with <see cref="Initialize"/> before using it.
    /// </summary>
    public class KeyManager
    {
        private static class Singleton
        {
            private static readonly object _lock = new();
            private static KeyManager _value;
            internal static KeyManager Value
            {
                get
                {
                    lock (_lock)
                    {
                        _value ??= new KeyManager();
                        return _value;
                    }
                }
            }
        }

        public static KeyManager Instance => Singleton.Value;

        private Web3KeyStore _keyStore;
        private Func<string, string> _encryptPassphraseFunc;
        private Func<string, string> _decryptPassphraseFunc;

        private PrivateKey _signedInPrivateKey;

        public bool IsInitialized => _keyStore is not null;
        public bool IsSignedIn => _signedInPrivateKey is not null;
        public PrivateKey SignedInPrivateKey => _signedInPrivateKey;
        public Address SignedInAddress => _signedInPrivateKey.Address;

        public void Initialize(
            string keyStorePath,
            Func<string, string> encryptPassphraseFunc,
            Func<string, string> decryptPassphraseFunc)
        {
            Debug.Log($"[KeyManager] Initialize(string, Func<string, string>, Func<string, string>) invoked: " +
                      $"{keyStorePath}");
            if (encryptPassphraseFunc is null)
            {
                Debug.LogError("[KeyManager] argument encryptPassphraseFunc is null.");
                return;
            }

            if (decryptPassphraseFunc is null)
            {
                Debug.LogError("[KeyManager] argument decryptPassphraseFunc is null.");
                return;
            }

            if (_keyStore is not null)
            {
                Debug.LogError("[KeyManager] KeyStore is already initialized.");
                return;
            }

            _keyStore = GetKeyStore(keyStorePath, fileNameOnMobile: "keystore");
            _encryptPassphraseFunc = encryptPassphraseFunc;
            _decryptPassphraseFunc = decryptPassphraseFunc;
            Debug.Log($"[KeyManager] Successfully initialize the key store: " +
                      $"{_keyStore.Path}");
        }

        #region Sign in
        /// <summary>
        /// Just sign in with the given private key hex.
        /// It does not register the key.
        /// </summary>
        public void SignIn(string privateKeyHex)
        {
            Debug.Log($"[KeyManager] SignIn(string) invoked with privateKeyHex.");
            if (string.IsNullOrEmpty(privateKeyHex))
            {
                Debug.LogError("[KeyManager] argument privateKeyHex is null or empty.");
                return;
            }

            var pk = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            SignIn(pk);
        }

        /// <summary>
        /// Just sign in with the given private key.
        /// It does not register the key.
        /// </summary>
        public void SignIn(PrivateKey privateKey)
        {
            Debug.Log($"[KeyManager] SignIn(PrivateKey) invoked: {privateKey.Address}");
            _signedInPrivateKey = privateKey;
        }

        /// <summary>
        /// Sign in with the given private key hex and register the key with the given passphrase.
        /// Replace the key when there is already a key registered with the same address.
        /// </summary>
        public void SignInAndRegister(
            string privateKeyHex,
            string passphrase,
            bool replaceWhenAlreadyRegistered = false)
        {
            Debug.Log($"[KeyManager] SignInAndRegister(string, string, bool) invoked.");
            SignIn(privateKeyHex);
            Register(privateKeyHex, passphrase, replaceWhenAlreadyRegistered);
        }

        /// <summary>
        /// Sign in with the given private key and register the key with the given passphrase.
        /// Replace the key when there is already a key registered with the same address.
        /// </summary>
        public void SignInAndRegister(
            PrivateKey privateKey,
            string passphrase,
            bool replaceWhenAlreadyRegistered = false)
        {
            Debug.Log($"[KeyManager] SignInAndRegister(PrivateKey, string, bool) invoked.");
            SignIn(privateKey);
            Register(privateKey, passphrase, replaceWhenAlreadyRegistered);
        }

        /// <summary>
        /// Try sign in with the first registered key in the key store with the given passphrase.
        /// </summary>
        public bool TrySigninWithTheFirstRegisteredKey(string passphrase)
        {
            Debug.Log($"[KeyManager] TrySigninWithTheFirstKey(string) invoked.");
            if (_keyStore is null)
            {
                Debug.LogWarning("[KeyManager] KeyStore is not initialized.");
                return false;
            }

            var firstKey = _keyStore.List().FirstOrDefault();
            if (firstKey is null)
            {
                Debug.LogWarning("[KeyManager] KeyStore does not have any key.");
                return false;
            }

            if (!TryUnprotect(firstKey.Item2, passphrase, out var privateKey))
            {
                return false;
            }

            _signedInPrivateKey = privateKey;
            return true;
        }
        #endregion Sign in

        #region Sign out
        /// <summary>
        /// Sign out.
        /// </summary>
        public void SignOut()
        {
            Debug.Log($"[KeyManager] SignOut() invoked.");
            _signedInPrivateKey = null;
        }
        #endregion Sign out

        #region IKeyStore as readonly
        public IEnumerable<Guid> GetListIds()
        {
            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return null;
            }

            return _keyStore.ListIds();
        }

        public IEnumerable<Tuple<Guid, ProtectedPrivateKey>> GetList()
        {
            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return null;
            }

            return _keyStore.List();
        }

        public bool TryGetProtectedPrivateKey(Guid id, out ProtectedPrivateKey protectedPrivateKey)
        {
            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                protectedPrivateKey = null;
                return false;
            }

            try
            {
                protectedPrivateKey = _keyStore.Get(id);
                return true;
            }
            catch (NoKeyException)
            {
                Debug.LogError($"[KeyManager] KeyStore does not have the key: {id}");
                protectedPrivateKey = null;
                return false;
            }
        }
        #endregion IKeyStore as readonly

        #region Register
        /// <summary>
        /// Register the key with the given private key hex and passphrase.
        /// Replace the key when there is already a key registered with the same address.
        /// </summary>
        public void Register(
            string privateKeyHex,
            string passphrase,
            bool replaceWhenAlreadyRegistered = false)
        {
            Debug.Log($"[KeyManager] Register(string, string, bool) invoked with privateKeyHex.");
            if (string.IsNullOrEmpty(privateKeyHex))
            {
                Debug.LogError("[KeyManager] argument privateKeyHex is null or empty.");
                return;
            }

            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return;
            }

            var pk = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            Register(pk, passphrase, replaceWhenAlreadyRegistered);
        }

        /// <summary>
        /// Register the key with the given private key and passphrase.
        /// Replace the key when there is already a key registered with the same address.
        /// </summary>
        public void Register(
            PrivateKey privateKey,
            string passphrase,
            bool replaceWhenAlreadyRegistered = false)
        {
            Debug.Log($"[KeyManager] Register(PrivateKey, string, bool) invoked");
            if (privateKey is null)
            {
                Debug.LogError("[KeyManager] argument privateKey is null.");
                return;
            }

            if (passphrase is null)
            {
                Debug.LogError("[KeyManager] argument passphrase is null.");
                return;
            }

            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return;
            }

            var ppk = ProtectedPrivateKey.Protect(privateKey, passphrase);
            Register(ppk, replaceWhenAlreadyRegistered);
        }

        /// <summary>
        /// Register the key with the given protected private key.
        /// Replace the key when there is already a key registered with the same address.
        /// </summary>
        public void Register(
            ProtectedPrivateKey protectedPrivateKey,
            bool replaceWhenAlreadyRegistered = false)
        {
            Debug.Log($"[KeyManager] Register(ProtectedPrivateKey, bool) invoked: " +
                      $"{protectedPrivateKey.Address}, " +
                      $"replaceWhenAlreadyRegistered({replaceWhenAlreadyRegistered})");
            if (protectedPrivateKey is null)
            {
                Debug.LogError("[KeyManager] argument protectedPrivateKey is null.");
                return;
            }

            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return;
            }

            if (Has(protectedPrivateKey.Address))
            {
                if (!replaceWhenAlreadyRegistered)
                {
                    Debug.LogError("[KeyManager] KeyStore already has the key: " +
                                   $"{protectedPrivateKey.Address}");
                    return;
                }

                Debug.Log($"[KeyManager] KeyStore already has the key: " +
                          $"{protectedPrivateKey.Address}. " +
                          $"Replace the key.");
                Unregister(protectedPrivateKey.Address);
            }

            _keyStore.Add(protectedPrivateKey);
            Debug.Log($"[KeyManager] Successfully register the key: " +
                      $"{protectedPrivateKey.Address}");
        }
        #endregion Register

        #region Unregister
        /// <summary>
        /// Unregister the key with the given address.
        /// </summary>
        public void Unregister(Address address)
        {
            Debug.Log($"[KeyManager] Unregister(Address) invoked: {address}");
            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return;
            }

            if (!TryGetKeyTuple(address, out var keyTuples))
            {
                Debug.LogError("[KeyManager] KeyStore does not have the key: " +
                               $"{address}");
                return;
            }

            foreach (var keyTuple in keyTuples)
            {
                _keyStore.Remove(keyTuple.Item1);
            }
        }
        #endregion Unregister

        #region Has
        /// <summary>
        /// Check if the key store has the key with the given private key hex.
        /// </summary>
        public bool Has(string privateKeyHex)
        {
            Debug.Log($"[KeyManager] Has(string) invoked with privateKeyHex.");
            if (string.IsNullOrEmpty(privateKeyHex))
            {
                Debug.LogError("[KeyManager] argument privateKeyHex is null or empty.");
                return false;
            }

            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return false;
            }

            var pk = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            return Has(pk.Address);
        }

        /// <summary>
        /// Check if the key store has the key with the given address.
        /// </summary>
        public bool Has(Address address)
        {
            Debug.Log($"[KeyManager] Has(Address) invoked with: {address}");
            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return false;
            }

            return _keyStore.List().Any(tuple => tuple.Item2.Address == address);
        }
        #endregion Has key

        #region Backup
        /// <summary>
        /// Backup the key with the given address to the given key store path.
        /// </summary>
        public void BackupKey(Address address, string keyStorePathToBackup)
        {
            Debug.Log($"[KeyManager] BackupKey(Address, string) invoked: {address}, " +
                      $"{keyStorePathToBackup}");
            if (_keyStore is null)
            {
                Debug.LogError("[KeyManager] KeyStore is not initialized.");
                return;
            }

            var targetKeyList = _keyStore.List()
                .Where(tuple => !tuple.Item2.Address.Equals(address))
                .ToList();
            var backupKeyStore = GetKeyStore(keyStorePathToBackup, fileNameOnMobile: "backup_keystore");
            foreach (var tuple in targetKeyList)
            {
                _keyStore.Remove(tuple.Item1);
                backupKeyStore.Add(tuple.Item2);
            }
        }
        #endregion Backup

        /// <summary>
        /// Try change the passphrase for the key with the given address.
        /// It will change the passphrase for all keys that match the address
        /// and the origin passphrase to the new passphrase.
        /// </summary>
        public bool TryChangePassphrase(
            Address address,
            string originPassphrase,
            string newPassphrase)
        {
            Debug.Log("[KeyManager] TryChangePassphrase(Address, string, string) invoked: " +
                      $"{address}");
            if (originPassphrase is null)
            {
                Debug.LogWarning("[KeyManager] argument originPassphrase is null.");
                return false;
            }

            if (newPassphrase is null)
            {
                Debug.LogWarning("[KeyManager] argument newPassphrase is null.");
                return false;
            }

            if (_keyStore is null)
            {
                Debug.LogWarning("[KeyManager] KeyStore is not initialized.");
                return false;
            }

            if (!TryGetKeyTuple(address, out var keyTuples))
            {
                Debug.LogWarning("[KeyManager] KeyStore does not have the key: " +
                                 $"{address}");
                return false;
            }

            var atLeastOneSuccess = false;
            foreach (var keyTuple in keyTuples)
            {
                if (!TryUnprotect(keyTuple.Item2, originPassphrase, out var privateKey))
                {
                    continue;
                }

                // NOTE: Why not use Unregister(Address)?
                //       This is because "UnregisterKey(Address)" deletes all keys in _keyStore
                //       that match the Address argument. Here we only delete keys that pass
                //       "TryUnprotect(ProtectedPrivateKey, string, out PrivateKey)".
                _keyStore.Remove(keyTuple.Item1);
                // NOTE: Why not use Register(PrivateKey, string, bool)?
                //       This is because "RegisterKey(PrivateKey, string, bool)" will either
                //       not register if there is a key equal to the PrivateKey argument
                //       among the keys already registered in _keyStore, or will delete
                //       and register all keys if the replace argument is true.
                _keyStore.Add(ProtectedPrivateKey.Protect(privateKey, newPassphrase));
                atLeastOneSuccess = true;
            }

            Debug.Log($"[KeyManager] Successfully change all passphrases for the key: " +
                      $"{address}");
            return atLeastOneSuccess;
        }

        /// <summary>
        /// Try unprotect the key with the given protected private key and passphrase.
        /// </summary>
        private bool TryUnprotect(
            ProtectedPrivateKey protectedPrivateKey,
            string passphrase,
            out PrivateKey privateKey)
        {
            Debug.Log($"[KeyManager] TryUnprotect(ProtectedPrivateKey, string, out PrivateKey) " +
                      $"invoked: {protectedPrivateKey.Address}");
            try
            {
                privateKey = protectedPrivateKey.Unprotect(passphrase);
                Debug.Log("[KeyManager] Successfully unprotect the key: " +
                          $"{protectedPrivateKey.Address}");
            }
            catch (IncorrectPassphraseException)
            {
                Debug.LogWarning("[KeyManager] The passphrase is incorrect for the key: " +
                                 $"{protectedPrivateKey.Address}");
                privateKey = null;
            }

            return privateKey is not null;
        }

        /// <summary>
        /// Try get the key tuple with the given address.
        /// </summary>
        private bool TryGetKeyTuple(
            Address address,
            out IEnumerable<Tuple<Guid, ProtectedPrivateKey>> keyTuple)
        {
            Debug.Log($"[KeyManager] TryGetKeyTuple(Address, out IEnumerable<Tuple<Guid, ProtectedPrivateKey>>) " +
                      $"invoked: {address}");
            try
            {
                keyTuple = _keyStore.List().Where(tuple =>
                    tuple.Item2.Address.Equals(address));
                return true;
            }
            catch (NoKeyException)
            {
                Debug.LogWarning($"[KeyManager] KeyStore does not have the key: {address}");
                keyTuple = null;
                return false;
            }
        }

        /// <summary>
        /// Get the key store with the given path.
        /// </summary>
        /// <param name="fileNameOnMobile">Used only on mobile platform when path is null.</param>
        private static Web3KeyStore GetKeyStore(string path, string fileNameOnMobile)
        {
            Debug.Log($"[KeyManager] GetKeyStore(string, string) invoked: {path}, " +
                      $"{fileNameOnMobile}");
            if (Platform.IsMobilePlatform())
            {
                path ??= Platform.GetPersistentDataPath(fileNameOnMobile);
                return new Web3KeyStore(path);
            }
            else
            {
                return path is null
                    ? Web3KeyStore.DefaultKeyStore
                    : new Web3KeyStore(path);
            }
        }

        private void CachePassPhrase(Address address, string passphrase)
        {
            if (passphrase is null)
            {
                Debug.LogError("[KeyManager] argument passphrase is null.");
                return;
            }

            var key = GetPlayerPrefsKey(address);
            var encryptedPassphrase = _encryptPassphraseFunc(passphrase);
            PlayerPrefs.SetString(key, encryptedPassphrase);
        }

        private string GetCachedPassPhrase(Address address)
        {
            var key = GetPlayerPrefsKey(address);
            var passphrase = PlayerPrefs.GetString(key, string.Empty);
            return _decryptPassphraseFunc(passphrase);
        }

        private static string GetPlayerPrefsKey(Address address)
        {
            return $"LOCAL_PASSPHRASE_{address}";
        }
    }
}
