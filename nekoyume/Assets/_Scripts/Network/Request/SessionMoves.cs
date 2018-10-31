using UnityEngine;

namespace Nekoyume.Network.Request
{
    [Route("session_moves")]
    [Method("post")]
    public class SessionMoves : Base
    {
        public string name = "";

        public SessionMoves()
        {
        }

        override public void ProcessResponse(string data)
        {
            Debug.Log("SessionMoves: " + name);
            NetworkManager.Instance.First(new InProgress() {
                Next = new LastStatus()
            });
        }
    }
}
