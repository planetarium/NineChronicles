using UnityEngine;


namespace Nekoyume.Network.Request
{
    [Route("login")]
    [Method("post")]
    public class Login : Base
    {
        public Login()
        {
        }

        override public void ProcessResponse(string data)
        {
            Debug.Log("Login: " + data);
            if (ResponseCallback != null)
            {
                ResponseCallback(data);
            }
        }
    }
}
