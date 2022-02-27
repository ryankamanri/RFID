using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RFID.Tags
{
	class Tag : Environment.TagObject
	{
		public TagState State { get; private set; }

		public ulong EPC { get; private set; }

		public Tag(ulong epc)
		{
			State = TagState.ReadyState;
			EPC = epc;
		}

		public Tag PowerOn()
		{
			State = TagState.ArbitrateState;
			Task.Run(() => { });
			return this;
		}

		public override void OnRequest(in byte[] require)
		{
            Console.WriteLine($"Tag {EPC} Receive Require {require[0]}");
			if (require[0] == EPC)
            {
                Console.WriteLine($"Tag {EPC} Reply {require[0]}");
				Environment.Send(this, require);
			}
				
		}
	}
}
