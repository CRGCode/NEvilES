using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;

namespace NEvilES.Server.ClientApi
{
	public class SendRequestWithRetry
	{
		private const int Retries = 3;
		private readonly TimeSpan TimeOut = TimeSpan.FromMilliseconds(2000);
		private readonly int attempt;

		private readonly Action<IEnumerable<byte>> replyHandler;
		private bool responseReceived;

		public SendRequestWithRetry(string address, IEnumerable<byte> msg,
		                            Action<IEnumerable<byte>> replyHandler)
		{
			this.replyHandler = replyHandler;

			responseReceived = false;
			attempt = 0;
			var message = msg.ToArray();
			do
			{
				using (var requester = new RequestSocket())
				{
					attempt++;
					requester.Options.Linger = TimeSpan.Zero;
					requester.Connect(address);
					requester.SendFrame(message);
					requester.ReceiveReady += item_PollInHandler;
					responseReceived = requester.Poll(TimeOut);
					requester.ReceiveReady -= item_PollInHandler;
					requester.Disconnect(address);
				}
			} while (attempt < Retries && !responseReceived);

			if (!responseReceived)
				throw new PermanentFailException(attempt);
		}

		private void item_PollInHandler(object sender, NetMQSocketEventArgs e)
		{
			responseReceived = true;
			var bytes = e.Socket.ReceiveFrameBytes();
			replyHandler(bytes);
		}
	}

	public static class SendRequest
	{
		public static void SendRequestWithoutRetry(string address, IEnumerable<byte> msg,
		                                           Action<IEnumerable<byte>> replyHandler)
		{
			var message = msg.ToArray();

			using (var requester = new RequestSocket())
			{
				requester.Connect(address);
				requester.Options.Linger = TimeSpan.Zero;
				requester.SendFrame(message);
				var bytes = requester.ReceiveFrameBytes();
				replyHandler(bytes);
				requester.Disconnect(address);
			}
		}
	}

	public class PermanentFailException : Exception
	{
		public PermanentFailException(int attempt) : base($"ZMQ Send Request failed after {attempt} attempts")
		{
		
		}
	}
}