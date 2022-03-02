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
            Console.WriteLine("Interrogator : On Conflict");
            //...
        }

        public override void Receive(in byte[] response)
		{
            Console.WriteLine($"Interrogator : Receive {response[0]}");
			if (response[0] == 1)
            {
                Console.WriteLine("Send 2");
				Environment.Send(this, new byte[] { 2 });
			}
				
			if(response[0] == 2)
                Console.WriteLine("Interrogator : End of Transmission");
		}


		public void Start()
		{
			var commandList = new List<ushort>()
			{
				Commands.Query,
				Commands.Query,
				Commands.ACK,
				Commands.Req_RN,
				Commands.Access,
				Commands.Kill
			};
			foreach (var command in commandList)
			{
				Console.WriteLine($"Interrogator : Send Command {command}");
				Environment.Send(this, BitConverter.GetBytes(command));
				Thread.Sleep(100);
			}
			
		}
	}
}
