using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using RFID.Tags;

namespace RFID.Interrogators
{
	class Interrogator : Environment.InterrogatorObject
	{
		private readonly byte[] _QArea = new byte[1000];
		private readonly int _QIndex = 0;

		private ushort RN16;

		private Task _QueryRepeatTask;

		private TagState _expectedTagState = TagState.ReadyState;

		public override void OnConflict()
		{
			Console.WriteLine("Interrogator : On Conflict");
			//...
		}

		public override void Receive(in byte[] response)
		{
			switch(_expectedTagState)
            {
				case TagState.ReadyState:
					OnReadyStateCommandReply(response);
					break;
				case TagState.ArbitrateState:
					OnArbitrateStateCommandReply(response);
					break;
				case TagState.ReplyState:
					OnReplyStateCommandReply(response);
					break;
				case TagState.AcknowledgedState:
					OnAcknowledgedStateCommandReply(response);
					break;
				case TagState.OpenState:
					OnOpenStateCommandReply(response);
					break;
				case TagState.SecuredState:
					OnSecuredStateCommandReply(response);
					break;
			}
		}

        private void OnReadyStateCommandReply(byte[] response)
        {
            Console.WriteLine("OnReadyStateCommandReply");

        }

        private void OnArbitrateStateCommandReply(byte[] response)
        {
			RN16 = BitConverter.ToUInt16(response.Take(2).ToArray());
		}

        private void OnReplyStateCommandReply(byte[] response)
        {
            throw new NotImplementedException();
        }

        private void OnAcknowledgedStateCommandReply(byte[] response)
        {
            throw new NotImplementedException();
        }

        private void OnOpenStateCommandReply(byte[] response)
        {
            throw new NotImplementedException();
        }

        private void OnSecuredStateCommandReply(byte[] response)
        {
            throw new NotImplementedException();
        }

        public void Start()
		{
			//var commandList = new List<ushort>()
			//{
			//    Commands.Query,
			//    Commands.Query,
			//    Commands.ACK,
			//    Commands.Req_RN,
			//    Commands.Access,
			//    Commands.Kill
			//};
			//foreach (var command in commandList)
			//{
			//    Console.WriteLine($"Interrogator : Send Command {command}");
			//    Environment.Send(this, BitConverter.GetBytes(command));
			//    Thread.Sleep(100);
			//}

			// Send Query Command. Only Simulate The Q Area.
			var command = Commands.Query;
			new Random().NextBytes(_QArea);
			var Q = _QArea[_QIndex];
			var commandBytes = BitConverter.GetBytes(command);
			var queryBytes = new byte[3];
			for (int i = 0; i < 2; i++) queryBytes[i] = commandBytes[i];
			queryBytes[2] = Q;
			// Make Tag At Arbitrate State.

			Console.WriteLine($"Interrogator : Send Command {command}, Command Bytes : {queryBytes[0]}, {queryBytes[1]}, Q = {queryBytes[2]}");
			Environment.Send(this, queryBytes);
			_expectedTagState = TagState.ArbitrateState;
			Environment.Send(this, queryBytes);

			// Recursive Send QueryRepeat
			_QueryRepeatTask = Task.Run(() =>
			{
				var queryRepCommand = BitConverter.GetBytes(Commands.QueryRep);
				while(true)
                {
					Environment.Send(this, queryRepCommand);
					Thread.Sleep(500);
				}
				
			});

		}

		
	}
}
