using System;
using System.Linq;
using System.Text;
using NEvilES.Server.Abstractions;

namespace NEvilES.Server.ClientApi
{
	public class StoreCounter //: I_ZMQCounter
	{
		private readonly string zmqHiLoCounterConnectionString;

		public StoreCounter(string protocol, string address, int port)
		{
			zmqHiLoCounterConnectionString = Global.ZMQConnectionString(protocol, address, port + 4);
		}

		public int ReadCounter()
		{
			var cmd = new byte[]{1};
			var counter = 0;
			new SendRequestWithRetry(zmqHiLoCounterConnectionString, cmd, r =>
				{
					var msg = Encoding.UTF8.GetString(r.ToArray());
					counter = int.Parse(msg);
				});
			return counter;
		}

		public void WriteCounter(int counter)
		{
			var request = Encoding.UTF8.GetBytes(counter.ToString());
			new SendRequestWithRetry(zmqHiLoCounterConnectionString, request, r =>
				{
					if(Encoding.UTF8.GetString(r.ToArray()).Equals("ERR"))
						throw new Exception("WriteCounter in client API recived an error");
				});
		}
	}
}