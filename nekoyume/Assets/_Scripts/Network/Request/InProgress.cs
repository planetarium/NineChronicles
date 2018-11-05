using UnityEngine;


namespace Nekoyume.Network.Request
{
    [Route("in_progress")]
    [Method("post")]
    public class InProgress<TResponse> : Base<TResponse> where TResponse : Response.Base
    {
        public int Count { get; set; }
        public Base<TResponse> Next
        {
            get; set;
        }

        public InProgress()
        {
            Count = 0;
        }

        override public void ProcessResponse(TResponse response)
        {
            if (response.message == "true")
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
