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

        override public void ProcessResponse(string data)
        {
            Debug.Log("Checksum: " + data);
        }
    }
}
