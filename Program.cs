using System;
using System.Threading;
using RFID.Environments;
using RFID.Tags;
using RFID.Interrogators;
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
			var tag1 = new Tag(1);
			var tag2 = new Tag(2);
			var tag3 = new Tag(3);
			var tag4 = new Tag(4);
			var tag5 = new Tag(5);
			var tag6 = new Tag(6);

			var interrogator = new Interrogator();

			var env = new Environment(interrogator, tag1, tag2, tag3, tag4, tag5, tag6);

            interrogator.Start();


            while (true) Thread.Sleep(500);
		}
	}
}
