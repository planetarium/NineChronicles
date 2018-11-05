using UnityEngine;


namespace Nekoyume.Network.Request
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class Route : System.Attribute
    {
        public string name;

        public Route(string name)
        {
            this.name = name;
        }
    }


    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class Method : System.Attribute
    {
        public string name;

        public Method(string name)
        {
            this.name = name;
        }
    }

    public abstract class ABase
    {
        
    }


    public class Base
    {
        public System.Action<Response.Base> ResponseCallback { get; set; }


        public Base()
        {
        }

        public string Route
        {
            get
            {
                System.Attribute[] attrs = System.Attribute.GetCustomAttributes(GetType());
                foreach (System.Attribute attr in attrs)
                {
                    if (attr is Network.Request.Route)
                    {
                        Network.Request.Route route = (Network.Request.Route)attr;
                        return route.name;
                    }
                }
                return "";
            }
        }

        public string Method
        {
            get
            {
                System.Attribute[] attrs = System.Attribute.GetCustomAttributes(GetType());
                foreach (System.Attribute attr in attrs)
                {
                    if (attr is Network.Request.Method)
                    {
                        Network.Request.Method method = (Network.Request.Method)attr;
                        return method.name;
                    }
                }
                return "get";
            }
        }

        public void Send()
        {
            NetworkManager.Instance.Push(this);
        }

        virtual public void DataHandle(string data)
        {
            Response.Base response = JsonUtility.FromJson<Response.Base>(data);
            ProcessResponse(response);
        }

        virtual public void ProcessResponse(Response.Base response)
        {
        }
    }

    public class Base<TResponse> : Base
    {
        public new System.Action<TResponse> ResponseCallback { get; set; }


        override public void DataHandle(string data)
        {
            TResponse response = JsonUtility.FromJson<TResponse>(data);
            ProcessResponse(response);
        }

        virtual public void ProcessResponse(TResponse response)
        {
        }
    }
}
