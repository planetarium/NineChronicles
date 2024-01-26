using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.EnumType;
using Nekoyume.Pattern;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Dcc : MonoSingleton<Dcc>
    {
        public static string DccVisible = "dcc_visible";
        public const int TimeOut = 10;

        public Dictionary<string, int> Avatars { get; private set; } = new();
        public bool IsConnected { get; set; }
        private readonly ConcurrentDictionary<int, Dictionary<DccPartsType, int>> _parts = new();

        public void Init(Dictionary<string, int> avatars)
        {
            Avatars = avatars;
        }

        public bool IsVisible(Address address, out int id, out bool isVisible)
        {
            var addr = address.ToString();
            if (Avatars is null || addr == "0x0000000000000000000000000000000000000000")
            {
                id = 0;
                isVisible = false;
                return false;
            }

            var isExistDcc = Avatars.ContainsKey(addr);
            var key = $"{DccVisible}_{addr}";
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, 1);
            }

            id = Avatars.ContainsKey(addr) ? Avatars[addr] : 0;
            isVisible = PlayerPrefs.GetInt(key, 0) > 0;
            return isExistDcc;
        }

        public void SetVisible(int value)
        {
            var avatarState = Game.instance.States.CurrentAvatarState;
            PlayerPrefs.SetInt($"{DccVisible}_{avatarState.address.ToString()}", value);
        }

        public async Task<Dictionary<DccPartsType, int>> GetParts(int dccId)
        {
            if (_parts.TryGetValue(dccId, out var parts))
            {
                return parts;
            }

            _parts.TryAdd(dccId, null);
            var dccParts = new Dictionary<DccPartsType, int>();
            var url = $"{Game.instance.URL.DccMetadata}{dccId}.json";
            var headerName = Game.instance.URL.DccEthChainHeaderName;
            var headerValue = Game.instance.URL.DccEthChainHeaderValue;

            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(TimeOut);
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add(headerName, headerValue);
            var resp = await client.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DccMetadata>(json);
                dccParts.Add(DccPartsType.background, result.traits[0]);
                dccParts.Add(DccPartsType.skin, result.traits[1]);
                dccParts.Add(DccPartsType.face, result.traits[2]);
                dccParts.Add(DccPartsType.ear_tail, result.traits[3]);
                dccParts.Add(DccPartsType.ac_face, result.traits[4]);
                dccParts.Add(DccPartsType.hair, result.traits[5]);
                dccParts.Add(DccPartsType.ac_eye, result.traits[6]);
                dccParts.Add(DccPartsType.ac_head, result.traits[7]);
                _parts[dccId] = dccParts;
            }

            return _parts[dccId];
        }
    }
}
