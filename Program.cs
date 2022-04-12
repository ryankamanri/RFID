using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RFID.Tags;
using RFID.Interrogators;
using Channel = RFID.Environments.Channel;
using Environment = RFID.Environments.Environment;
using MyPlotHelper;


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
			EmulateAndPlot();
			Thread.Sleep(10000);
		}

		private static void EmulateAndPlot()
		{
			var emulateCount = 40;
			var X = new double[emulateCount];
			var Y = new double[emulateCount];
			
			for (var i = 20; i <= emulateCount; i++)
			{
				Console.WriteLine($"********************************************* The {i} Emulate *********************************************");
				Thread.Sleep(3000);
				var consequence = EmulateOnce(i);
				consequence.Print();
				X[i-1] = i;
				Y[i-1] = consequence.ChannelOccupyRatio;
			}

			Console.WriteLine("*************************************************************************************************");
			Console.WriteLine($"Emulate {emulateCount} Times");
			Console.WriteLine("Start To Call MatlabPlot...");

			var plot = new MatlabPlot();
			
			plot.Plot2(X, Y)
				.AddLabel(MatlabPlot.ReadyPlot.LabelLocation.X, "Tag Count")
				.AddLabel(MatlabPlot.ReadyPlot.LabelLocation.Y, "Channel Occupy Ratio")
				.AddLegend("Emulate The Relation Of Tag Count And The Channel Occupy Ratio")
				.Execute();
		}

		private static Consequence EmulateOnce(int tagCount)
		{
			var tags = new List<Tag>();

			for (var i = 1; i <= tagCount; i++)
			{
				tags.Add(new Tag((ulong)i, (ulong)i));
			}

			var interrogator = new Interrogator();
			
			var channel = new Channel();

			var env = new Environment(channel, interrogator, tags.ToArray());

			var watch = new Stopwatch();

			var elapsedTime = new TimeSpan();

			var openTagCount = 0;
			
			interrogator.OnOpen += (environment, handle) =>
			{
				watch.Stop();
				elapsedTime = watch.Elapsed;
				Console.WriteLine("===============================================================================================");
				Console.WriteLine($"Used Time {watch.Elapsed}");
				Console.WriteLine($"Opened The {++openTagCount} Connection With Handle {handle}");
				Console.WriteLine("===============================================================================================");

				Task.Run(() => environment.Send(interrogator, BitConverter.GetBytes(Commands.Access)));
				Thread.Sleep(Environment.Object.SLOT_TIME);
				Task.Run(() => environment.Send(interrogator, BitConverter.GetBytes(Commands.Kill)));
				watch.Start();
			};
			
			watch.Start();
			
			interrogator.Start();
			
			Thread.Sleep(Environment.Object.SLOT_TIME);

			var consequence = new Consequence(
				tagCount: tags.Count,
				totalElapsedTime: elapsedTime,
				totalOccupiedSlot: (int)(env.Channel.TotalOccupiedTime * 2 / Environment.Object.SLOT_TIME),
				channelOccupyRatio: env.Channel.TotalOccupiedTime * 2 / elapsedTime.TotalMilliseconds);
			

			return consequence;

		}
		
		private readonly struct Consequence
		{
			public int TagCount { get; }
			public TimeSpan TotalElapsedTime { get; }
			public int TotalOccupiedSlot { get; }
			public double ChannelOccupyRatio { get; }

			public Consequence(int tagCount, TimeSpan totalElapsedTime, int totalOccupiedSlot, double channelOccupyRatio)
			{
				TagCount = tagCount;
				TotalElapsedTime = totalElapsedTime;
				TotalOccupiedSlot = totalOccupiedSlot;
				ChannelOccupyRatio = channelOccupyRatio;
			}

			public void Print()
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("===============================================================================================");
				Console.WriteLine($"Tag Count: {TagCount}");
				Console.WriteLine($"Total Elapsed Time : {TotalElapsedTime}");
				Console.WriteLine($"Total Occupied Slot : {TotalOccupiedSlot} * {Environment.Object.SLOT_TIME} ms");
				Console.WriteLine($"Channel Occupy Ratio : {ChannelOccupyRatio}");
				Console.WriteLine("===============================================================================================");
			}
		}


	}
}
