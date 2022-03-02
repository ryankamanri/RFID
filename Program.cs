using System;
using System.Threading;
using RFID.Tags;
using RFID.Interrogators;
namespace RFID
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			var tag1 = new Tag(1);
			var tag2 = new Tag(2);
			var tag3 = new Tag(3);

			var interrogator = new Interrogator();

			var env = new Environment(interrogator, tag1, tag2, tag3);
			
			interrogator.Start();
			
			while (true) Thread.Sleep(500);
		}
	}
}
