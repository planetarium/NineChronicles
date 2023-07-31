#nullable enable
using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Formatters;
using Lib9c.Renderers;
using Libplanet.Crypto;
using Libplanet.Action.State;
using MessagePack;

namespace Nekoyume.Action
{
    [MessagePackObject]
    public struct NCActionEvaluation
    {
#pragma warning disable MsgPack003
        [Key(0)]
        [MessagePackFormatter(typeof(NCActionFormatter))]
        public ActionBase? Action { get; set; }

        [Key(1)]
        [MessagePackFormatter(typeof(AddressFormatter))]
        public Address Signer { get; set; }
#pragma warning restore MsgPack003

        [Key(2)]
        public long BlockIndex { get; set; }

        [Key(3)]
        [MessagePackFormatter(typeof(AccountStateDeltaFormatter))]
        public IAccountStateDelta OutputState { get; set; }

        [Key(4)]
        [MessagePackFormatter(typeof(ExceptionFormatter<Exception>))]
        public Exception? Exception { get; set; }

        [Key(5)]
        [MessagePackFormatter(typeof(AccountStateDeltaFormatter))]
        public IAccountStateDelta PreviousState { get; set; }

        [Key(6)]
        public int RandomSeed { get; set; }

        [Key(7)]
        public Dictionary<string, IValue> Extra { get; set; }


        [SerializationConstructor]
        public NCActionEvaluation(
            ActionBase? action,
            Address signer,
            long blockIndex,
            IAccountStateDelta outputStates,
            Exception? exception,
            IAccountStateDelta previousStates,
            int randomSeed,
            Dictionary<string, IValue> extra
        )
        {
            Action = action;
            Signer = signer;
            BlockIndex = blockIndex;
            OutputState = outputStates;
            Exception = exception;
            PreviousState = previousStates;
            RandomSeed = randomSeed;
            Extra = extra;
        }

        public ActionEvaluation<ActionBase> ToActionEvaluation()
        {
            return new ActionEvaluation<ActionBase>
            {
                Action =  Action is null ? new RewardGold() : Action,
                Signer = Signer,
                BlockIndex = BlockIndex,
                OutputState = OutputState,
                Exception = Exception,
                PreviousState = PreviousState,
                RandomSeed = RandomSeed,
                Extra = Extra
            };
        }
    }
}
