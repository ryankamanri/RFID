using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using RFID.Tags;
using RFID.Interrogators;
using Channel = RFID.Environments.Channel;
using Environment = RFID.Environments.Environment;

/// <summary>
///  An RFID EPCglobal Transmission Protocol Class 1 Generation 2 Emulator.
///  
/// Author : Ryan Kamanri
/// </summary>
namespace RFID
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			var tag1 = new Tag(1,1);
			var tag2 = new Tag(2,2);
			var tag3 = new Tag(3,3);
			var tag4 = new Tag(4,4);
			var tag5 = new Tag(5,5);
			var tag6 = new Tag(6,6);

			var interrogator = new Interrogator();

			var channel = new Channel();

			var env = new Environment(channel, interrogator, tag1, tag2, tag3, tag4, tag5, tag6);

			var watch = new Stopwatch();
			
			interrogator.OnOpen += async() =>
			{
				watch.Stop();
				Console.WriteLine("===============================================================================================");
				Console.WriteLine($"Totally Use Time {watch.Elapsed}");
				Console.WriteLine("Opened Connection!!");
				Console.WriteLine("===============================================================================================");
				return BitConverter.GetBytes(Commands.Read);
			};
			
			watch.Start();
			
            interrogator.Start();
			
            

            Thread.Sleep(10000);
		}
	}
}
