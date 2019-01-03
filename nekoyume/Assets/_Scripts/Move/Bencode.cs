using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BencodeNET.Objects;
using BencodeNET.Parsing;

namespace ShouldBeRemoved {
    public static class BencodeExtensions
    {
        public static byte[] ToBencoded(this object o)
        {
            var bo = o.ToBObject();
            return bo.EncodeAsBytes();
        }

        private static IBObject ToBObject(this object o)
        {
            if(o is string)
            { 
                return new BString((string) o);
            }
            else if (o is int || o is long)
            {
                var x = Convert.ToInt64(o);
                return new BNumber(x);
            }
            else if (o is byte[])
            {
                return new BString((byte[]) o);
            }
            else 
            {
                if (IsList(o))
                {
                    var asEnumerable = ((IEnumerable)o).Cast<object>();
                    return new BList(asEnumerable.Select(e => e.ToBObject()));
                }
                else if (IsDictionary(o))
                {
                    var asDict = (IDictionary)o;
                    var rv = new BDictionary();
                    foreach (var key in asDict.Keys)
                    {
                        rv[new BString((string)key)] = asDict[key].ToBObject();
                    }
                    return rv;
                }
            }

            throw new Exception();
        }

        private static bool IsList(object o)
        {
            return IsGenericType(o, typeof(IList<>));
        }

        private static bool IsDictionary(object o)
        {
            return IsGenericType(o, typeof(IDictionary<,>));
        }

        private static bool IsGenericType(object o, Type t)
        {
            var type = o.GetType();

            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == t);
        }
    }

}