using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Network.Request
{
    [Route("checksum")]
    [Method("get")]
    public class Checksum : Base
    {
        public Checksum()
        {
        }

        override public void ProcessResponse(Response.Base response)
        {
            Debug.Log("Checksum: " + response);
        }
    }
}
