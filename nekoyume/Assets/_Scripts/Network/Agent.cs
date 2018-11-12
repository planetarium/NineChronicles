using System;
using System.Collections;
using System.Collections.Generic;
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
        public event EventHandler<IAction> DidReceiveAction;
        private readonly string apiUrl;
        private readonly PrivateKey privateKey;
        private float interval;
        private List<Move.Move> moves;

        public Agent(string apiUrl, PrivateKey privateKey, float interval = 1.0f)
        {
            if (string.IsNullOrEmpty(apiUrl))
            {
                throw new ArgumentException("apiUrl should not be empty or null.", nameof(apiUrl));
            }

            this.apiUrl = apiUrl;
            this.privateKey = privateKey;
            this.interval = interval;
            moves = new List<Move.Move>();
        }

        void Flush()
        {
        }

        public void Send(Move.Move action)
        {
            moves.Add(action);
        }

        public IEnumerable<Move.Move> Moves
        {
            get
            {
                return moves;
            }
        }

        public IEnumerator Run()
        {
            int? lastBlockOffset = null;
            while (true)
            {
                yield return new WaitForSeconds(interval);
                var url = string.Format(
                    "{0}/users/0x{1}/moves/", apiUrl, privateKey.PublicKey.ToAddress().Hex()
                );

                if (lastBlockOffset.HasValue)
                {
                    url += string.Format("?block_offset={0}", lastBlockOffset);
                }
                var www = UnityWebRequest.Get(url);
                yield return www.SendWebRequest();
                var jsonPayload = www.downloadHandler.text;
                var converters = new Newtonsoft.Json.JsonConverter[]{
                    new Move.JSONConverter()
                };
                var response = JsonConvert.DeserializeObject<Response>(jsonPayload, new JsonSerializerSettings
                {
                    Converters = converters
                });
            }
        }
    }
}
