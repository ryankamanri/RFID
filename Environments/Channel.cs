using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RFID.Environments
{
	public class Channel
	{
		public bool IsOccupied { get => RestOccupiedMilliseconds != 0; }

		public int RestOccupiedMilliseconds { get; private set; } = 0;

		public Channel()
		{
			Task.Run(() =>
			{
				while(true)
				{
					if(RestOccupiedMilliseconds != 0)
					{
						Thread.Sleep(10);
						RestOccupiedMilliseconds-=10;
					}
				}
			});
		}
		public void Occupy(int milliseconds)
		{
			RestOccupiedMilliseconds += milliseconds;
		}
		

	}
}
