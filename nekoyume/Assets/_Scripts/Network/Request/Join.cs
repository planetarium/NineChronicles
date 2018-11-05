using UnityEngine;


namespace Nekoyume.Network.Request
{
    [Route("join")]
    [Method("post")]
    public class Join : Base<Response.Login>
    {
        public string name = "";

        public Join()
        {
        }

        override public void ProcessResponse(Response.Base response)
        {
            NetworkManager.Instance.First(new InProgress<Response.Login>() {
                Next = new Login() {
                    ResponseCallback = this.ResponseCallback,
                }
            });
        }
    }
}
