using System;
using RFID.Tags;
using RFID.Interrogators;
namespace RFID
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			var tag1 = new Tag(1).PowerOn();
			var tag2 = new Tag(2).PowerOn();

			var interrogator = new Interrogator();

			var env = new Environment(interrogator, tag1, tag2);

			interrogator.Start();
			
		}
	}
}
