using System;

namespace NEvilES.Server.Abstractions;

public static class Global
{
    public static string ZMQConnectionString(string protocol, string address, int port)
    {
        return $"{protocol}://{address}:{port}";
    }
}