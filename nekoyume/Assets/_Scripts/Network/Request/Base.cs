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

    public class Base
    {
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

        virtual public void ProcessResponse(string data)
        {
        }
    }
}