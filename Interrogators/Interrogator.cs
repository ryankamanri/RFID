using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using RFID.Tags;
using RFID.Environments;
using Environment = RFID.Environments.Environment;

namespace RFID.Interrogators
{
	class Interrogator : Environment.InterrogatorObject
	{
		
		private const int QUERY_REPEAT_TIMES = 3;

		private readonly byte[] _QArea = new byte[1000];
		private int _QIndex = 0;

		private ushort RN16 = ushort.MaxValue;

		private Task _queryRepeatTask;
		private bool _isQueryRepeatTaskCanceled = false;
		private bool _isReceiveTaskCanceled = false;

		

		

		public event Func<Task<byte[]>> OnOpen; 
		public event Func<byte[], Task<byte[]>> OnReceive;

		public Interrogator()
		{
			new Random().NextBytes(_QArea);
		}

		public override void OnConflict()
		{
			// Cancel The Query Repeat Task And Receive Task.
			_isQueryRepeatTaskCanceled = true;
			_isReceiveTaskCanceled = true;
			Log("ON CONFLICT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
		}

		public override void Receive(in byte[] response)
		{
			Thread.Sleep(INTERVAL);
			if (_isReceiveTaskCanceled)
			{
				_isReceiveTaskCanceled = false;
				return;
			}
			switch (_expectedTagState)
			{
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


		private void OnArbitrateStateCommandReply(byte[] response)
		{
			_expectedTagState = TagState.ReplyState;
			RN16 = BitConverter.ToUInt16(response.Take(2).ToArray());
			Log($"Receive RN16 = {RN16}");
			// Cancel The Query Repeat Task
			_isQueryRepeatTaskCanceled = true;

			// Send The ACK Command With RN16
			
			Log($"Send ACK With RN16 = {RN16}");
			var ACKCommand = BitConverter.GetBytes(Commands.ACK).Concat(BitConverter.GetBytes(RN16));
			Environment.Send(this, ACKCommand);
		}

		private void OnReplyStateCommandReply(byte[] response)
		{
			_expectedTagState = TagState.AcknowledgedState;
			var EPC = BitConverter.ToInt64(response.Take(8).ToArray());
			Log($"Receive EPC = {EPC}");
			// Send Req_RN with RN16
			var commandBytes = BitConverter.GetBytes(Commands.Req_RN).Concat(BitConverter.GetBytes(RN16));
			Environment.Send(this, commandBytes);
		}

		private async void OnAcknowledgedStateCommandReply(byte[] response)
		{
			_expectedTagState = TagState.OpenState;
			Log($"Receive Handle : {BitConverter.ToUInt16(response)}");
			var commandBytes = await OnOpen();
			Log($"Send Command");
			Environment.Send(this, commandBytes);
		}

		private async void OnOpenStateCommandReply(byte[] response)
		{
			Log("Receive On OpenState");
			// Environment.Send(this, await OnReceive(response));
		}

		private void OnSecuredStateCommandReply(byte[] response)
		{
			throw new NotImplementedException();
		}

		public void Start()
		{

			// Send Query Command. Only Simulate The Q Area.
			var command = Commands.Query;
			
			var Q = _QArea[_QIndex];
			var commandBytes = BitConverter.GetBytes(command);
			var queryBytes = commandBytes.Concat(Q);
			// Make Tag At Arbitrate State.

			Log($"Send Query Command, Q = {queryBytes[2] & 0x0f }");
			Environment.Send(this, queryBytes);
			Thread.Sleep(INTERVAL);

			_expectedTagState = TagState.ArbitrateState;
			Log($"Send Query Command, Q = {queryBytes[2] & 0x0f }");
			Environment.Send(this, queryBytes);
			Thread.Sleep(INTERVAL);

			// Recursive Send QueryRepeat Until Be Canceled
			_isQueryRepeatTaskCanceled = false;

            var queryRepCommand = BitConverter.GetBytes(Commands.QueryRep);
            while (Q >= 0)
            {
                for (var i = 0; i < QUERY_REPEAT_TIMES; i++)
                {
                    if (_isQueryRepeatTaskCanceled) return;
                    Log($"Send QueryRepeat Command");
                    Environment.Send(this, queryRepCommand);
                    Thread.Sleep(REPEAT_INTERVAL);
                }
				// Q-1, Send Query Adjust Sign.
				if (_isQueryRepeatTaskCanceled) return;
                Log("Send QueryAdjust Command, Subtract 1");
                Q--;
                var queryAdjustCommand = BitConverter.GetBytes(Commands.QueryAdjust).Concat(Commands.QueryAdjust_Down);
                Environment.Send(this, queryAdjustCommand);
                Thread.Sleep(REPEAT_INTERVAL);

            }



        }


    }
}
