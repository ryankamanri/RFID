using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace RFID.Interrogators
{
	class Interrogator : Environment.InterrogatorObject
	{
        public override void OnConflict()
        {
            Console.WriteLine("On Conflict");
        }

        public override void Receive(in byte[] response)
		{
            Console.WriteLine($"Reader Receive {response[0]}");
			if (response[0] == 1)
            {
                Console.WriteLine("Send 2");
				Environment.Send(this, new byte[] { 2 });
			}
				
			if(response[0] == 2)
                Console.WriteLine("End of Transmission");
		}


		public void Start()
		{
            Console.WriteLine("Reader Send 1");
			Environment.Send(this, new byte[] { 1 });
			while (true) Thread.Sleep(500);
		}
	}
}
