using UnityEngine;


namespace Nekoyume.Network.Request
{
    [Route("last_status")]
    [Method("post")]
    public class LastStatus : Base<Response.LastStatus>
    {
        public LastStatus()
        {
        }

        override public void ProcessResponse(Response.LastStatus response)
        {
            if (ResponseCallback != null)
            {
                ResponseCallback(response);
            }
        }
    }
}
