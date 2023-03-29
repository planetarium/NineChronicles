using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Consensus;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("op_validator_set_1")]
    public sealed class ValidatorSetOperate
        : GameAction, IEquatable<ValidatorSetOperate>
    {
        public ValidatorSetOperate()
        {
            Error = "An uninitialized action.";
        }

        public ValidatorSetOperate(ValidatorSetOperatorType @operator, Validator operand)
        {
            Error = null;
            Operator = @operator;
            Operand = operand;
        }

        public string Error { get; private set; }

        public ValidatorSetOperatorType Operator { get; private set; }

        public Validator Operand { get; private set; }

        public static ValidatorSetOperate Append(Validator operand) =>
            new ValidatorSetOperate(ValidatorSetOperatorType.Append, operand);

        public static ValidatorSetOperate Remove(Validator operand) =>
            new ValidatorSetOperate(ValidatorSetOperatorType.Remove, operand);

        public static ValidatorSetOperate Update(Validator operand) =>
            new ValidatorSetOperate(ValidatorSetOperatorType.Update, operand);

        public static readonly string ValidatorSetOperateKey = "vsok";

        public bool Equals(ValidatorSetOperate other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Error == other.Error &&
                   Operator == other.Operator &&
                   Operand.Equals(other.Operand);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) ||
                   (obj is ValidatorSetOperate other && Equals(other));
        }

        public override int GetHashCode() =>
            (Error, (int)Operator, Operand).GetHashCode();

        public override IAccountStateDelta Execute(IActionContext context)
        {
             if (Error != null)
             {
                 throw new InvalidOperationException(Error);
             }

             CheckPermission(context);

             IAccountStateDelta previousState = context.PreviousStates;
             ValidatorSet validatorSet = previousState.GetValidatorSet();

             Func<ValidatorSet, Validator, Validator> func = Operator.ToFunc();
             return previousState.SetValidator(func(validatorSet, Operand));
        }

        public override IValue PlainValue =>
            (Error is null)
                ? (IValue)Bencodex.Types.Dictionary.Empty
                    .Add(
                        "op",
                        Operator is ValidatorSetOperatorType op
                            ? new Text(op.ToString())
                            : (IValue)Null.Value
                    )
                    .Add("operand", Operand.Encoded)
                : (Text)Error;

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is Text t)
            {
                Error = t;
                return;
            }

            if (!(plainValue is Dictionary d))
            {
                Error =
                    "The action serialization is invalid; " +
                    "the serialization should be a dictionary.";
                return;
            }

            if (!d.TryGetValue((Text)"op", out IValue opValue))
            {
                Error = "The serialized dictionary lacks the key \"op\".";
                return;
            }

            if (!(opValue is Text opText))
            {
                Error = "The serialized \"op\" field is not a text.";
                return;
            }

            string opStr = opText.Value;
            if (!Enum.TryParse(opStr, true, out ValidatorSetOperatorType op))
            {
                Error = $"The serialized operator \"{opStr}\" is invalid.";
                return;
            }

            if (!d.TryGetValue((Text)"operand", out IValue operandValue))
            {
                Error = "The serialized dictionary lacks the key \"operand\".";
                return;
            }

            if (!(operandValue is Bencodex.Types.Dictionary operandDict))
            {
                Error = "The serialized \"operand\" field is not an dictionary.";
                return;
            }

            Operator = op;
            Operand = new Validator(operandDict);
            Error = null;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [ValidatorSetOperateKey] = PlainValue
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            if (plainValue.TryGetValue(ValidatorSetOperateKey, out IValue rawValue))
            {
                LoadPlainValue(rawValue);
            }

            Error =
                $"The serialized dictionary lacks the key \"{nameof(ValidatorSetOperateKey)}\".";
        }
    }
}
