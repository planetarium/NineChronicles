using UnityEngine;


namespace Nekoyume.Network.Request
{
    [Route("session_moves")]
    [Method("post")]
    public class SessionMoves : Base<Response.LastStatus>
    {
        public string name = "";

        public SessionMoves()
        {
        }

        override public void ProcessResponse(Response.Base data)
        {
            NetworkManager.Instance.First(new InProgress<Response.LastStatus>() {
                Next = new LastStatus() {
                    ResponseCallback = this.ResponseCallback,
                }
            });
        }
    }
}
