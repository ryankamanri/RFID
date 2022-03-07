using RFID.Environments;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Environment = RFID.Environments.Environment;

namespace RFID.Tags
{
	class Tag : Environment.TagObject
	{
		public TagState State { get; private set; }

		public ulong EPC { get; private set; }

		private int Q = ushort.MaxValue;
		private int Slot = int.MaxValue;

		private ushort RN16 = ushort.MaxValue;
		private bool _isACKAccessed = false;

		public Tag(ulong epc)
		{
			State = TagState.ReadyState;
			EPC = epc;
		}
		
		private void Log(string message)
		{
			Console.WriteLine($"Tag${EPC}({State}) : {message}");
		}
		
		/// <summary>
		/// Receive Request From Interrogator
		/// The 2-Bytes At Head Is Designated Command
		/// </summary>
		/// <param name="environment"></param>
		/// <param name="request"></param>
		public override void OnRequest(Environment environment, in byte[] request)
		{
			
			
			// //////////////////////////////////////////////////////////////////////////
			// // Start to Receive Message
			
			if (request == default || request.Length < 2)
			{
				Log("Invalid Request");
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
					Log($"Unmatched Command : {command}, Execute Default Task");
					break;
					
			}

		}

		private void OnReadyStateCommand(Environment environment, ushort command, in byte[] request)
		{
			//Log("Matched Command On Ready State");
			Log("Matched Command On Ready State");
			switch (command)
			{
				case Commands.Query:
					Log("Matched Query Command");
					State = TagState.ArbitrateState;
					Log("State => ArbitrateState");
					//...
					break;
				default:
					Log($"Ignore Command {command}");
					break;
			}
			
		}
		private void OnArbitrateStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Log("Matched Command On Arbitrate State");
			switch (command)
			{
				case Commands.Query:
					Log("Matched Query Command");
					// Check The Q Value Which Is At Index 2
					Q = request[2] & 0x0f;
					Slot = new Random().Next(0, (int)Math.Pow(2, Q));
					Log($"Slot = {Slot}");
					//...
					break;
				case Commands.Select:
					Log("Matched Select Command");
					State = TagState.ReadyState;
					Log("State => ReadyState");
					//...
					break;
				case Commands.QueryAdjust:
				case Commands.QueryRep:
					Log("Matched QueryAdjust/QueryRep Command");
					if (Slot != 0)
					{
						if(command == Commands.QueryAdjust)
						{
							// Adjust Q By Command UpDn Code
							switch(request[2] & 0x07)
							{
								case Commands.QueryAdjust_Up:
									Q++;
									break;
								case Commands.QueryAdjust_Down:
									Q--;
									break;
							}
							// Reset Slot
							Slot = new Random().Next(0, (int)Math.Pow(2, Q));
							Log($"Reset Slot = {Slot}");
						}
						Slot--;
						Log($"Slot = {Slot}");
						break;
					}
					// If Slot = 0, Reply RN16, And State = Reply
					// Using Pure Aloha, Resend Reply After A Random Time
					Log("State => ReplyState");
					State = TagState.ReplyState;
					RN16 = (ushort)new Random().Next();
					var replyBytes = BitConverter.GetBytes(RN16);
					
					while(!_isACKAccessed)
					{
						Log($"Sending RN16 {RN16}");
						environment.Send(this, replyBytes);
						Thread.Sleep(new Random().Next() & 0x0fff);// 0 - 4s
					}
					
					// ...
					break;
				default:
					Log($"Ignore Command {command}");
					break;
				
			}
		}
		private void OnReplyStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Log("Matched Command On Reply State");
			switch (command)
			{
				case Commands.ACK:
					Log("Matched ACK Command");
					// Cancel Send RN16
					_isACKAccessed = true;
					// Examine Whether The RN16 Is Correct
					var rn16 = BitConverter.ToUInt16(request.Skip(2).Take(2).ToArray());
					Log($"Receive RN16 = {rn16}");
					if(RN16 != rn16)
					{
						Log("Incorrect RN16");
						State = TagState.ArbitrateState;
						Log("State => ArbitrateState");
						break;
					}
					State = TagState.AcknowledgedState;
					Log("State => AcknowledgedState");
					// Send EPC + CRC ...
					
					//...
					break;
				case Commands.Query:
					Log("Matched Query Command");
					break;
				case Commands.QueryAdjust:
					Log("Matched QueryAdjust Command");
					// Return A New RN16
					RN16 = (ushort)new Random().Next();
					var replyBytes = BitConverter.GetBytes(RN16);
					Log($"Send New RN16 {RN16}");
					environment.Send(this, replyBytes);
					//...
					break;
				case Commands.Select:
					Log("Matched Select Command");
					State = TagState.ReadyState;
					Log("State => ReadyState");
					//...
					break;
				default:
					Log($"No Valid Command ({command})");
					State = TagState.ArbitrateState;
					Log("State => ArbitrateState");
					//...
					break;
				
			}
		}
		private void OnAcknowledgedStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Log("Matched Command On Acknowledged State");
			switch (command)
			{
				case Commands.Req_RN:
					Log("Matched Req_RN Command");
					State = TagState.OpenState;
					Log("State => OpenState");
					//...
					break;
				case Commands.ACK:
					Log("Matched ACK Command");
					//...
					break;
				case Commands.Select:
				case Commands.QueryRep: 
				case Commands.QueryAdjust:
					Log("Matched Select/QueryRep/QueryAdjust Command");
					State = TagState.ReadyState;
					Log("State => ReadyState");
					//...
					break;
				default:
					Log($"No Valid Command ({command})");
					State = TagState.ArbitrateState;
					Log("State => ArbitrateState");
					//...
					break;
				
			}
		}
		private void OnOpenStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Log("Matched Command On Open State");
			switch (command)
			{
				case Commands.Access:
					Log("Matched Access Command");
					State = TagState.SecuredState;
					Log("State => SecuredState");
					//...
					break;
				case Commands.Req_RN:
					Log("Matched Req_RN Command");
					//...
					break;
				case Commands.Read:
					Log("Matched Read Command");
					//...
					break;
				case Commands.Write:
					Log("Matched Write Command");
					//...
					break;
				case Commands.Lock:
					Log("Matched Lock Command");
					//...
					break;
				case Commands.Select:
				case Commands.QueryRep: 
				case Commands.QueryAdjust:
					Log("Matched Select/QueryRep/QueryAdjust Command");
					State = TagState.ReadyState;
					Log("State => ReadyState");
					//...
					break;
				
			}
		}
		private void OnSecuredStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Log("Matched Command On Secured State");
			switch (command)
			{
				case Commands.Kill:
					Log("Matched Kill Command");
					State = TagState.KilledState;
					Log("State => KilledState");
					//...
					break;
				case Commands.Req_RN:
					Log("Matched Req_RN Command");
					//...
					break;
				case Commands.Read:
					Log("Matched Read Command");
					//...
					break;
				case Commands.Write:
					Log("Matched Write Command");
					//...
					break;
				case Commands.Lock:
					Log("Matched Lock Command");
					//...
					break;
				case Commands.Select:
				case Commands.QueryRep: 
				case Commands.QueryAdjust:
					Log("Matched Select/QueryRep/QueryAdjust Command");
					State = TagState.ReadyState;
					Log("State => ReadyState");
					//...
					break;
			}
		}
	}
}
