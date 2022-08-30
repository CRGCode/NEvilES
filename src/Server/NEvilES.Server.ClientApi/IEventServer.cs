using System;
using NEvilES.Server.Abstractions;

namespace NEvilES.Server.ClientApi
{
	public interface IEventServer
	{
		LoadEventsReply ReadStream(Guid id);
		void WriteStream(StoreEventsRequest commit);
	}
}