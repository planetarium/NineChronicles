using UnityEngine;

namespace Nekoyume.Network.Request
{
    [Route("login")]
    public class Login : Base
    {
        public Login()
        {
        }

        override public void ProcessResponse(string data)
        {
            Debug.Log("Login: " + data);
        }
    }
}
