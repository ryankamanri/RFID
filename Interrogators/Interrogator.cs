using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        private int _conflictCountEveryQueryRep = 0;

        private int _tagThreadCount = 0;
        private Mutex _tagThreadMutex = new Mutex();

        private Channel _channel = new Channel();

        public event Action<Environment, ushort> OnOpen;
        public event Func<byte[], Task<byte[]>> OnReceive;

        public Interrogator()
        {
            new Random().NextBytes(_QArea);
            
        }

        public override void OnConflict()
        {
            Log(
                $"ON CONFLICT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!, Thread : {Thread.CurrentThread.ManagedThreadId}");
            // Cancel The Receive Task. And Send QueryAdjust (Q++)
            ref var Q = ref _QArea[0];
            _isReceiveTaskCanceled = true;
            Log("_isReceiveTaskCanceled => true");
            
            
            
            if (_conflictCountEveryQueryRep >= (Q & 0x0f) / 4)
            {
                
                // _QArea[0] = _QArea[++_QIndex];
                // var queryCommand = BitConverter.GetBytes(Commands.Query).Concat(_QArea[0]);
                // Environment.Send(this, queryCommand);
                // Log($"Send New Query Command, Q = {queryCommand[2] & 0x0f}");
                Task.Run(() =>
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    Thread.Sleep(SLOT_TIME);
                    if(_channel.IsOccupied) return;
                    Log("AFTER SLOT_TIME _isReceiveTaskCanceled => false");
                    _isReceiveTaskCanceled = false;
                });
                
            }
            
           
        }

        public override void Receive(in byte[] response)
        {
            // // Here Should Make Sure That The Next Action MUST Execute After Receive All RN16s.
            // if (_isReceiveTaskCanceled)
            // {
            //     Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            //     _channel.Occupy(SLOT_TIME);
            //     Thread.Sleep(SLOT_TIME);
            //     Thread.CurrentThread.Priority = ThreadPriority.Normal;
            //     Log($"Rest Occupy Time : {_channel.RestOccupiedMilliseconds}");
            //     if(_channel.IsOccupied) return;
            //     _isReceiveTaskCanceled = false;
            //     Log("AFTER SLOT_TIME _isReceiveTaskCanceled => false");
            //     return;
            // }
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
                case TagState.ReadyState:
                    break;
                case TagState.KilledState:
                    break;
            }
        }


        private void OnArbitrateStateCommandReply(byte[] response)
        {
            
            RN16 = BitConverter.ToUInt16(response.Take(2).ToArray());
            Log($"Receive RN16 = {RN16}, Thread : {Thread.CurrentThread.ManagedThreadId}");
            
            // Send The ACK Command With RN16
            var ACKCommand = BitConverter.GetBytes(Commands.ACK).Concat(BitConverter.GetBytes(RN16));
            
            // Here Should Make Sure That The Next Action MUST Execute After Receive All RN16s.
            // Origin Way: Low Ratio Of Success

            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            Thread.Sleep(HALF_SLOT_TIME);
            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            
            
            // If Interrupt Happened, Cancel Sending
            Log($"AFTER HALF_SLOT_TIME _isReceiveTaskCanceled : {_isReceiveTaskCanceled}");
            Log($"Pause The Query Repeat Task");
            // Cancel The Query Repeat Task
            _isQueryRepeatTaskCanceled = true;
            
            if (_isReceiveTaskCanceled)
            {
                // Log("_isReceiveTaskCanceled => false");
                // _isReceiveTaskCanceled = false;
                
                _QArea[0] = _QArea[++_QIndex];
                var queryCommand = BitConverter.GetBytes(Commands.Query).Concat(_QArea[0]);
                Environment.Send(this, queryCommand);
                Log($"Send New Query Command, Q = {queryCommand[2] & 0x0f}");
                Thread.Sleep(SLOT_TIME);
                Log("Resume The Query Repeat Task");
                _isQueryRepeatTaskCanceled = false;
                return;
            }
            
            
            _expectedTagState = TagState.ReplyState;
            Log($"Send ACK With RN16 = {RN16}");
            
            Environment.Send(this, ACKCommand);
        }

        private void OnReplyStateCommandReply(byte[] response)
        {
            _expectedTagState = TagState.AcknowledgedState;
            var EPC = BitConverter.ToInt64(response.Take(8).ToArray());
            Log($"Receive EPC = {EPC}, Send Req_RN With RN16 {RN16}");
            // Send Req_RN with RN16
            var commandBytes = BitConverter.GetBytes(Commands.Req_RN).Concat(BitConverter.GetBytes(RN16));
            Environment.Send(this, commandBytes);
        }

        private async void OnAcknowledgedStateCommandReply(byte[] response)
        {
            _expectedTagState = TagState.OpenState;
            var handle = BitConverter.ToUInt16(response);
            Log($"Receive Handle : {handle}");
            OnOpen(Environment, handle);
            _isQueryRepeatTaskCanceled = false;
            _expectedTagState = TagState.ArbitrateState;
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

            ref var Q = ref _QArea[0];
            var commandBytes = BitConverter.GetBytes(command);
            var queryCommand = commandBytes.Concat(Q);
            // Make Tag At Arbitrate State.

            Log($"Send Query Command, Q = {queryCommand[2] & 0x0f}");
            Environment.Send(this, queryCommand);
            Thread.Sleep(SLOT_TIME);

            _expectedTagState = TagState.ArbitrateState;
            Log($"Send Query Command, Q = {queryCommand[2] & 0x0f}");
            Environment.Send(this, queryCommand);
            Thread.Sleep(SLOT_TIME);

            // Recursive Send QueryRepeat Until Be Canceled
            _isQueryRepeatTaskCanceled = false;

            QueryRepeatTask(ref Q);
            
        }

        public void QueryRepeatTask(ref byte Q)
        {
            var queryRepCommand = BitConverter.GetBytes(Commands.QueryRep);
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            do
            {
                restart:
                for (var i = 0; i <= QUERY_REPEAT_TIMES; i++)
                {

                    do Thread.Sleep(SLOT_TIME);
                    while (_isQueryRepeatTaskCanceled);
                    Log($"Send QueryRepeat Command");
                    Environment.Send(this, queryRepCommand);
                    _isReceiveTaskCanceled = false;
                    _conflictCountEveryQueryRep = 0;
                }

                // Q-1, Send Query Adjust Sign.
                
                // do Thread.Sleep(SLOT_TIME);
                // while (_isQueryRepeatTaskCanceled);
                Thread.Sleep(SLOT_TIME);
                if(_isQueryRepeatTaskCanceled) goto restart; // Reset The Query Repeat Task.
                
                Log($"Send QueryAdjust Command, Q({Q & 0x0f}) Subtract 1");
                Q--;
                var queryAdjustCommand = BitConverter.GetBytes(Commands.QueryAdjust).Concat(Commands.QueryAdjust_Down);
                Environment.Send(this, queryAdjustCommand);
                _isReceiveTaskCanceled = false;
            } while ((Q & 0x0f) < 0x0f);
        }
    }
}