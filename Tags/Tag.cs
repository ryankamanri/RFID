using System;
using System.Collections.Generic;
using System.Linq;
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
		
		
		/// <summary>
		/// Receive Request From Interrogator
		/// The 2-Bytes At Head Is Designated Command
		/// </summary>
		/// <param name="environment"></param>
		/// <param name="request"></param>
		public override void OnRequest(Environment environment, in byte[] request)
		{
			////////////////////////////////////////////////////////////
			// Function Test
			
			// Console.WriteLine($"Tag {EPC} Receive Require {request[0]}");
			// if (request[0] == EPC)
			// {
			// 	Console.WriteLine($"Tag {EPC} Reply {request[0]}");
			// 	environment.Send(this, request);
			// }
			
			
			// //////////////////////////////////////////////////////////////////////////
			// // Start to Receive Message
			
			if (request == default || request.Length < 2)
			{
				Console.WriteLine($"Tag : Invalid Request");
				return;
			}
			var command = BitConverter.ToUInt16(request.Take(2).ToArray());
			switch (State)
			{
				case TagState.ReadyState:
					OnReadyStateCommand(environment, command, request);
					break;
				case TagState.ArbitrateState:
					OnArbitrateStateCommand(environment, command, request);
					break;
				case TagState.ReplyState:
					OnReplyStateCommand(environment, command, request);
					break;
				case TagState.AcknowledgedState:
					OnAcknowledgedStateCommand(environment, command, request);
					break;
				case TagState.OpenState:
					OnOpenStateCommand(environment, command, request);
					break;
				case TagState.SecuredState:
					OnSecuredStateCommand(environment, command, request);
					break;
				default:
					Console.WriteLine($"Tag : Unmatched Command : {command}, Execute Default Task");
					break;
					
			}

		}

		private void OnReadyStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Console.WriteLine($"Tag${EPC} : Matched Command On Ready State");
			switch (command)
			{
				case Commands.Query:
					Console.WriteLine($"Tag${EPC} : Matched Query Command");
					State = TagState.ArbitrateState;
					Console.WriteLine($"Tag${EPC} : State => ArbitrateState");
					//...
					break;
				default:
					Console.WriteLine($"Tag${EPC} : Ignore Command {command}");
					break;
			}
			
		}
		private void OnArbitrateStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Console.WriteLine($"Tag${EPC} : Matched Command On Arbitrate State");
			switch (command)
			{
				case Commands.Query:
					Console.WriteLine($"Tag${EPC} : Matched Query Command");
					// If Slot = 0, Reply RN16, And State = Reply
					State = TagState.ReplyState;
					Console.WriteLine($"Tag${EPC} : State => ReplyState");
					//...
					break;
				case Commands.Select:
					Console.WriteLine($"Tag${EPC} : Matched Select Command");
					State = TagState.ReadyState;
					Console.WriteLine($"Tag${EPC} : State => ReadyState");
					//...
					break;
				case Commands.QueryRep:
					Console.WriteLine($"Tag${EPC} : Matched QueryRep Command");
					//...
					break;
				default:
					Console.WriteLine($"Tag${EPC} : Ignore Command {command}");
					break;
				
			}
		}
		private void OnReplyStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Console.WriteLine($"Tag${EPC} : Matched Command On Reply State");
			switch (command)
			{
				case Commands.ACK:
					Console.WriteLine($"Tag${EPC} : Matched ACK Command");
					State = TagState.AcknowledgedState;
					Console.WriteLine($"Tag${EPC} : State => AcknowledgedState");
					//...
					break;
				case Commands.QueryAdjust:
					Console.WriteLine($"Tag${EPC} : Matched QueryAdjust Command");
					//...
					break;
				case Commands.Select:
					Console.WriteLine($"Tag${EPC} : Matched Select Command");
					State = TagState.ReadyState;
					Console.WriteLine($"Tag${EPC} : State => ReadyState");
					//...
					break;
				default:
					Console.WriteLine($"Tag${EPC} : No Valid Command ({command})");
					State = TagState.ArbitrateState;
					Console.WriteLine($"Tag${EPC} : State => ArbitrateState");
					//...
					break;
				
			}
		}
		private void OnAcknowledgedStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Console.WriteLine($"Tag${EPC} : Matched Command On Acknowledged State");
			switch (command)
			{
				case Commands.Req_RN:
					Console.WriteLine($"Tag${EPC} : Matched Req_RN Command");
					State = TagState.OpenState;
					Console.WriteLine($"Tag${EPC} : State => OpenState");
					//...
					break;
				case Commands.ACK:
					Console.WriteLine($"Tag${EPC} : Matched ACK Command");
					//...
					break;
				case Commands.Select:
				case Commands.QueryRep: 
				case Commands.QueryAdjust:
					Console.WriteLine($"Tag${EPC} : Matched Select/QueryRep/QueryAdjust Command");
					State = TagState.ReadyState;
					Console.WriteLine($"Tag${EPC} : State => ReadyState");
					//...
					break;
				default:
					Console.WriteLine($"Tag${EPC} : No Valid Command ({command})");
					State = TagState.ArbitrateState;
					Console.WriteLine($"Tag${EPC} : State => ArbitrateState");
					//...
					break;
				
			}
		}
		private void OnOpenStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Console.WriteLine($"Tag${EPC} : Matched Command On Open State");
			switch (command)
			{
				case Commands.Access:
					Console.WriteLine($"Tag${EPC} : Matched Access Command");
					State = TagState.SecuredState;
					Console.WriteLine($"Tag${EPC} : State => SecuredState");
					//...
					break;
				case Commands.Req_RN:
					Console.WriteLine($"Tag${EPC} : Matched Req_RN Command");
					//...
					break;
				case Commands.Read:
					Console.WriteLine($"Tag${EPC} : Matched Read Command");
					//...
					break;
				case Commands.Write:
					Console.WriteLine($"Tag${EPC} : Matched Write Command");
					//...
					break;
				case Commands.Lock:
					Console.WriteLine($"Tag${EPC} : Matched Lock Command");
					//...
					break;
				case Commands.Select:
				case Commands.QueryRep: 
				case Commands.QueryAdjust:
					Console.WriteLine($"Tag${EPC} : Matched Select/QueryRep/QueryAdjust Command");
					State = TagState.ReadyState;
					Console.WriteLine($"Tag${EPC} : State => ReadyState");
					//...
					break;
				
			}
		}
		private void OnSecuredStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Console.WriteLine($"Tag${EPC} : Matched Command On Secured State");
			switch (command)
			{
				case Commands.Kill:
					Console.WriteLine($"Tag${EPC} : Matched Kill Command");
					State = TagState.KilledState;
					Console.WriteLine($"Tag${EPC} : State => KilledState");
					//...
					break;
				case Commands.Req_RN:
					Console.WriteLine($"Tag${EPC} : Matched Req_RN Command");
					//...
					break;
				case Commands.Read:
					Console.WriteLine($"Tag${EPC} : Matched Read Command");
					//...
					break;
				case Commands.Write:
					Console.WriteLine($"Tag${EPC} : Matched Write Command");
					//...
					break;
				case Commands.Lock:
					Console.WriteLine($"Tag${EPC} : Matched Lock Command");
					//...
					break;
				case Commands.Select:
				case Commands.QueryRep: 
				case Commands.QueryAdjust:
					Console.WriteLine($"Tag${EPC} : Matched Select/QueryRep/QueryAdjust Command");
					State = TagState.ReadyState;
					Console.WriteLine($"Tag${EPC} : State => ReadyState");
					//...
					break;
			}
		}
	}
}
