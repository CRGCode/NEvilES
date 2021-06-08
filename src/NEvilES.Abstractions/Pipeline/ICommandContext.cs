using System;

namespace NEvilES.Abstractions.Pipeline
{
    public interface ICommandContext
    {
        IUser By { get; set; }
        ITransaction Transaction { get; set; }
        IUser ImpersonatorBy { get; set; }
        string AppVersion { get; set; }
        ICommandResult Result { get; set; }
    }

    public interface IUser
    {
        Guid GuidId { get; }
    }
}