using UnityEngine;

namespace Nekoyume.Network.Request
{
    [Route("join")]
    [Method("post")]
    public class Join : Base
    {
        public string name = "";

        public Join()
        {
        }

        override public void ProcessResponse(string data)
        {
            Debug.Log("Join: " + data);
            NetworkManager.Instance.First(new InProgress() {
                Next = new Login()
            });
        }
    }
}
