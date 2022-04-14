using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RFID.Environments
{
	public class Channel
	{
		public long TotalOccupiedTime { get; set; }
		public bool IsOccupied => RestOccupiedMilliseconds > 0;

		public long RestOccupiedMilliseconds => _occupySeconds - _watch.ElapsedMilliseconds;

		private Stopwatch _watch;

		private long _occupySeconds;

		public Channel()
		{
			_watch = Stopwatch.StartNew();
		}
		public void Occupy(int milliseconds)
		{
			_occupySeconds = _watch.ElapsedMilliseconds + milliseconds;
			TotalOccupiedTime += milliseconds;
		}

		public void CancelOccupy()
		{
			_occupySeconds = _watch.ElapsedMilliseconds;
			TotalOccupiedTime -= RestOccupiedMilliseconds;
		} 


	}
}
