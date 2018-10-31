using UnityEngine;

namespace Nekoyume.Network.Request
{
    [Route("in_progress")]
    [Method("post")]
    public class InProgress : Base
    {
        public int Count { get; set; }
        public Base Next
        {
            get; set;
        }

        public InProgress()
        {
            Count = 0;
        }

        override public void ProcessResponse(string data)
        {
            var json = JsonUtility.FromJson<Response.Base>(data);
            if (json.message == "true")
            {
                Count++;
                Debug.Log("InProgress ... " + Count.ToString());
                NetworkManager.Instance.First(this);
            }
            else
            {
                Debug.Log("Progress Complete");
                if (Next != null)
                    NetworkManager.Instance.First(Next);
            }
        }
    }
}
