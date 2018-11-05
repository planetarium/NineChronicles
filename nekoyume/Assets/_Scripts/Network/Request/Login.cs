using UnityEngine;


namespace Nekoyume.Network.Request
{
    [Route("login")]
    [Method("post")]
    public class Login : Base<Response.Login>
    {
        public Login()
        {
        }

        override public void ProcessResponse(Response.Login response)
        {
            if (ResponseCallback != null)
            {
                ResponseCallback(response);
            }
        }
    }
}
