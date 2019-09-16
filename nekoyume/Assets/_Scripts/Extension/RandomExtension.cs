using Libplanet.Action;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public static class RandomExtension
    {
        public static Guid GenerateUUID4(this IRandom random)
        {
            var b = new byte[16];
            random.NextBytes(b);
            return new Guid(b);
        }
    }
}
