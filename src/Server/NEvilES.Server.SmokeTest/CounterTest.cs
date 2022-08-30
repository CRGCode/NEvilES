using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRG.ES.SmokeTest
{
	class CounterTest
	{
		private readonly int total;
		private readonly IPersistCounter[] counters;
		private const int CountUpTo = 200;

		public CounterTest(int total)
		{
			this.total = total;
			counters = Enumerable.Range(0, total).Select(i => new NamedPersistentCounter(i + ".counter")).ToArray();

			var files = Directory.GetFiles(".", "*.counter");
			Array.ForEach(files, f =>
			{
				var counter = f.Substring(2,f.LastIndexOf('.')-f.LastIndexOf('\\')-1);
				var i = int.Parse(counter);
				if (counters[i].ReadCounter() == CountUpTo)
				{
					Console.WriteLine("Count {0} good to Delete()", i);
				}
				File.Delete(f);
			});
		}

		public void SmokeIt()
		{
			var tasks = new Task[total];
			foreach(var i in Enumerable.Range(0, total))
			{
				tasks[i] = Task.Factory.StartNew(() =>
				{
					IncrementCounter(counters[i]);
				});
			}

			Task.WaitAll(tasks);

			foreach(var i in Enumerable.Range(0, total))
			{
				var finalValue = counters[i].ReadCounter();
				if(finalValue != CountUpTo)
				{
					Debugger.Break();
				}
				Console.WriteLine("{0} - {1}",i,finalValue);
			}
			Console.ReadKey();
		}

		private static void IncrementCounter(IPersistCounter namedPersistentCounter)
		{
			var rand = new Random();
			foreach (var i in Enumerable.Range(0, CountUpTo))
			{
				// do some work
				var counter = namedPersistentCounter.LockCounter();
				Thread.Sleep(rand.Next(3));
				namedPersistentCounter.ReleaseCounter(counter + 1);
			}
		}
	}
}
