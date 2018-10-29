using UnityEngine;

namespace Nekoyume.Network.Request
{
    [Route("last_status")]
    public class LastStatus : Base
    {
        public LastStatus()
        {
        }

        override public void ProcessResponse(string data)
        {
            Debug.Log("LastStatus: " + data);
        }
    }
}
