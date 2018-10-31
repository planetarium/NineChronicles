using UnityEngine;


namespace Nekoyume.Network.Request
{
    [Route("join")]
    [Method("post")]
    public class Join : InProgress
    {
        public string name = "";

        public Join()
        {
        }

        override public void ProcessResponse(string data)
        {
            NetworkManager.Instance.First(new InProgress() {
                Next = new Login() {
                    ResponseCallback = this.ResponseCallback,
                }
            });
        }
    }
}
