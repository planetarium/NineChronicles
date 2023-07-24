using System;
using Libplanet.Types.Consensus;

namespace Nekoyume.Action
{
    public static class ValidatorSetOperatorTypeExtensions
    {
        public static Func<ValidatorSet, Validator, Validator>
            ToFunc(this ValidatorSetOperatorType @operator) => @operator switch
        {
            ValidatorSetOperatorType.Append => (set, validator) =>
            {
                if (set.PublicKeys.Contains(validator.PublicKey))
                {
                    throw new InvalidOperationException(
                        "Cannot append validator when its already exist.");
                }

                return validator;
            },
            ValidatorSetOperatorType.Remove => (set, validator) =>
            {
                if (set.PublicKeys.Contains(validator.PublicKey))
                {
                    return new Validator(validator.PublicKey, 0);
                }

                throw new InvalidOperationException(
                    "Cannot remove validator when its do not exist.");
            },
            ValidatorSetOperatorType.Update => (set, validator) =>
            {
                if (set.PublicKeys.Contains(validator.PublicKey))
                {
                    return validator;
                }

                throw new InvalidOperationException(
                    "Cannot update validator when its do not exist.");
            },
            _ => throw new ArgumentException("Unsupported operator: " + @operator,
                nameof(@operator))
        };
    }
}
