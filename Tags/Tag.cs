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
		

		private int _Q = ushort.MaxValue;
		private int _Slot = int.MaxValue;

		private ushort _RN16 = ushort.MaxValue;
		private ushort _handle = ushort.MaxValue;
		private bool _isReplyCanceled = false;

		public Tag(ulong epc, ulong pid)
		{
			State = TagState.ReadyState;
			EPC = epc;
			PID = pid;
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
					_Q = request[2] & 0x0f;
					_Slot = Rand.U16(0, (int)Math.Pow(2, _Q));
					Log($"Slot = {_Slot}");
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
					if (_Slot != 0)
					{
						if(command == Commands.QueryAdjust)
						{
							// Adjust Q By Command UpDn Code
							switch(request[2] & 0x07)
							{
								case Commands.QueryAdjust_Up:
									_Q++;
									break;
								case Commands.QueryAdjust_Down:
									_Q--;
									break;
							}
							// Reset Slot
							_Slot = Rand.U16(0, (int)Math.Pow(2, _Q));
							Log($"Reset Slot = {_Slot}");
						}
						_Slot--;
						Log($"Slot = {_Slot}");
						break;
					}
					// If Slot = 0, Reply RN16, And State = Reply
					// Using Pure Aloha, Resend Reply After A Random Time
					Log("State => ReplyState", ConsoleColor.Yellow);
					State = TagState.ReplyState;
					_RN16 = Rand.U16();
					var replyBytes = BitConverter.GetBytes(_RN16);
					
					while(!_isReplyCanceled)
					{
						Log($"Sending RN16 {_RN16}", ConsoleColor.Yellow);
						environment.Send(this, replyBytes);
						Thread.Sleep(Rand.U16(0xff ,0xfff));// 256 - 4, 096 ms
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
			Log("Matched Command On Reply State", ConsoleColor.Yellow);
			// Cancel Send RN16
			_isReplyCanceled = true;
			switch (command)
			{
				case Commands.ACK:
					Log("Matched ACK Command",ConsoleColor.Yellow);
					
					// Examine Whether The RN16 Is Correct
					var rn16 = BitConverter.ToUInt16(request.Skip(2).Take(2).ToArray());
					Log($"Receive RN16 = {rn16}" ,ConsoleColor.Yellow);
					if(_RN16 != rn16)
					{
						Log("Incorrect RN16", ConsoleColor.Yellow);
						State = TagState.ArbitrateState;
						Log("State => ArbitrateState" ,ConsoleColor.Yellow);
						break;
					}
					State = TagState.AcknowledgedState;
					Log("State => AcknowledgedState", ConsoleColor.Yellow);
					// Thread.Sleep(INTERVAL);
					// Send Backscatter EPC + CRC ...
					Log($"Send Backscatter EPC : {EPC}", ConsoleColor.Yellow);
					var backScatter = BitConverter.GetBytes(EPC);
					environment.Send(this, backScatter);
					//...
					break;
				case Commands.Query:
					Log("Matched Query Command" ,ConsoleColor.Yellow);
					break;
				case Commands.QueryAdjust:
					Log("Matched QueryAdjust Command", ConsoleColor.Yellow);
					// Return A New RN16
					_RN16 = Rand.U16();
					var replyBytes = BitConverter.GetBytes(_RN16);
					Log($"Send New RN16 {_RN16}" ,ConsoleColor.Yellow);
					environment.Send(this, replyBytes);
					//...
					break;
				case Commands.Select:
					Log("Matched Select Command", ConsoleColor.Yellow);
					State = TagState.ReadyState;
					Log("State => ReadyState", ConsoleColor.Yellow);
					//...
					break;
				default:
					Log($"No Valid Command ({command})", ConsoleColor.Yellow);
					State = TagState.ArbitrateState;
					Log("State => ArbitrateState", ConsoleColor.Yellow);
					//...
					break;
				
			}
		}
		private void OnAcknowledgedStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Log("Matched Command On Acknowledged State", ConsoleColor.Blue);
			switch (command)
			{
				case Commands.Req_RN:
					Log("Matched Req_RN Command", ConsoleColor.Blue);
					var rn16 = BitConverter.ToUInt16(request, 2);
					if (rn16 != _RN16)
					{
						Log($"Incorrect RN16 : ${rn16}", ConsoleColor.Blue);
						break;
					}
					State = TagState.OpenState;
					Log("State => OpenState", ConsoleColor.Blue);
					// Return Handle
					// Thread.Sleep(INTERVAL);
					_handle = Rand.U16();
					Log($"Return Handle {_handle}", ConsoleColor.Blue);
					environment.Send(this, BitConverter.GetBytes(_handle));
					//...
					break;
				case Commands.ACK:
					Log("Matched ACK Command", ConsoleColor.Blue);
					//...
					break;
				case Commands.Select:
				case Commands.QueryRep: 
				case Commands.QueryAdjust:
					Log("Matched Select/QueryRep/QueryAdjust Command" ,ConsoleColor.Blue);
					State = TagState.ReadyState;
					Log("State => ReadyState", ConsoleColor.Blue);
					//...
					break;
				default:
					Log($"No Valid Command ({command})", ConsoleColor.Blue);
					State = TagState.ArbitrateState;
					Log("State => ArbitrateState", ConsoleColor.Blue);
					//...
					break;
				
			}
		}
		private void OnOpenStateCommand(Environment environment, ushort command, in byte[] request)
		{
			Log("Matched Command On Open State", ConsoleColor.Green);
			switch (command)
			{
				case Commands.Access:
					Log("Matched Access Command", ConsoleColor.Green);
					State = TagState.SecuredState;
					Log("State => SecuredState", ConsoleColor.Green);
					//...
					break;
				case Commands.Req_RN:
					Log("Matched Req_RN Command", ConsoleColor.Green);
					//...
					break;
				case Commands.Read:
					Log("Matched Read Command", ConsoleColor.Green);
					//...
					break;
				case Commands.Write:
					Log("Matched Write Command", ConsoleColor.Green);
					//...
					break;
				case Commands.Lock:
					Log("Matched Lock Command", ConsoleColor.Green);
					//...
					break;
				case Commands.Select:
				case Commands.QueryRep: 
				case Commands.QueryAdjust:
					Log("Matched Select/QueryRep/QueryAdjust Command", ConsoleColor.Green);
					State = TagState.ReadyState;
					Log("State => ReadyState", ConsoleColor.Green);
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
