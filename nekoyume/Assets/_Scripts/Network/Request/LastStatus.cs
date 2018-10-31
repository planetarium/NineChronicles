using UnityEngine;


namespace Nekoyume.Network.Request
{
    [Route("last_status")]
    [Method("post")]
    public class LastStatus : Base
    {
        public LastStatus()
        {
        }

        override public void ProcessResponse(string data)
        {
            Debug.Log("LastStatus: " + data);
            if (ResponseCallback != null)
            {
                ResponseCallback(data);
            }
        }
    }
}
