using System.Collections.Generic;
using System.Linq;

namespace NEvilES.DataStore.Marten
{
    public interface IConnectionString
    {
        string Data { get; }
        Dictionary<string, string> Keys { get; }
    }

    public class ConnectionString : IConnectionString
    {
        public Dictionary<string, string> Keys { get; }
        public string Data { get; }

        public ConnectionString(string connectionString)
        {
            Data = connectionString;
            Keys = Data.Split(';').ToDictionary(k => k.Split('=')[0], v => v.Split('=')[1]);
        }
    }
}