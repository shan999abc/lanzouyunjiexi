using System.Web.Script.Serialization;

namespace Sht
{
    internal class Json
    {
        static readonly JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();
        internal static object DeserializeObject(string JsonText)
        {
            return JavaScriptSerializer.DeserializeObject(JsonText);
        }
    }
}
