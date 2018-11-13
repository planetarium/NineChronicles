using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Nekoyume.Move;
using Newtonsoft.Json;
using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using Planetarium.SDK.Action;
using Planetarium.SDK.Address;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.Network.Agent
{
    [Serializable]
    internal class Response
    {
        public ResultCode result = ResultCode.ERROR;
        public List<Move.Move> moves;
    }

    public class Agent
    {
        public event EventHandler<Move.Move> DidReceiveAction;
        private readonly string apiUrl;
        private readonly PrivateKey privateKey;
        private float interval;
        private OrderedDictionary moves;
        public byte[] UserAddress => privateKey.ToAddress();

        public Agent(string apiUrl, PrivateKey privateKey, float interval = 1.0f)
        {
            if (string.IsNullOrEmpty(apiUrl))
            {
                throw new ArgumentException("apiUrl should not be empty or null.", nameof(apiUrl));
            }

            this.apiUrl = apiUrl;
            this.privateKey = privateKey;
            this.interval = interval;
            this.moves = new OrderedDictionary();
        }

        public void Send(Move.Move move)
        {
            move.Sign(privateKey);
            moves.Add(move.Id, move);
        }

        public IEnumerable<Move.Move> Moves => moves.Values.Cast<Move.Move>();

        public IEnumerator Run()
        {
            long? lastBlockOffset = null;
            var serializerSettings = new JsonSerializerSettings
            {
                Converters = new Newtonsoft.Json.JsonConverter[]{
                        new Move.JSONConverter()
                }
            };

            while (true)
            {
                yield return new WaitForSeconds(interval);
                var url = string.Format(
                    "{0}/users/0x{1}/moves/", apiUrl, privateKey.ToAddress().Hex()
                );

                if (lastBlockOffset.HasValue)
                {
                    url += string.Format("?block_offset={0}", lastBlockOffset);
                }
                var www = UnityWebRequest.Get(url);
                yield return www.SendWebRequest();
                if (!www.isNetworkError)
                {
                    var jsonPayload = www.downloadHandler.text;
                    var response = JsonConvert.DeserializeObject<Response>(jsonPayload, serializerSettings);
                    foreach (var m in response.moves)
                    {
                        DidReceiveAction?.Invoke(this, m);
                        moves.Add(m);
                        lastBlockOffset = m.BlockId;
                    }
                }
            }
        }
    }
}
