using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable All

namespace RFID.Environments
{
	public class Environment
	{
		private const int SEND_FRAME_TIME = Object.HALF_SLOT_TIME;
		
		private const bool IS_LOG_INTERREGATOR = true;
		private const bool IS_LOG_TAG = false;
		
		public class Object
		{
			/// <summary>
			/// Send Message Interval 
			/// </summary>
			public const int HALF_SLOT_TIME = SLOT_TIME / 2;

			public const int SLOT_TIME = 100;
		}

		public abstract class InterrogatorObject : Object
		{
			protected TagState _expectedTagState = TagState.ReadyState;
			/// <summary>
			/// The Environment Consist Of Several `TagObject` And This `ReaderObject`
			/// ** A `ReaderObject` Can And Only Can Located In 1 `Environment` **
			/// </summary>
			public Environment Environment { get; private set; }

			public void SetEnvironment(Environment environment) => Environment = environment;
			/// <summary>
			/// Set Handler While Reader Receiving Message From Tag
			/// </summary>
			/// <param name="response"></param>
			public abstract void Receive(in byte[] response);
			/// <summary>
			/// Set Handler While Reader Receiving CONFLICT Message From Several Tags
			/// </summary>
			public abstract void OnConflict();
			
			protected void Log(string message, ConsoleColor color = ConsoleColor.Red)
			{
				if(!IS_LOG_INTERREGATOR) return;
				var originColor = Console.ForegroundColor;
				if (originColor != color) Console.ForegroundColor = color;
				Console.WriteLine($"Interrogator(Expect {_expectedTagState}) : {message}");
				Console.ForegroundColor = originColor;
			}
		}

		public abstract class TagObject : Object
		{
			public TagState State { get; protected set; }

			public ulong EPC { get; protected set; }
			public ulong PID { get; protected set; }
			/// <summary>
			/// Set Handler While Tag Receiving Request From Reader
			/// </summary>
			/// <param name="request"></param>
			public abstract void OnRequest(Environment environment, in byte[] request);
			
			protected void Log(string message, ConsoleColor color = ConsoleColor.White)
			{
				if(!IS_LOG_TAG) return;
				var originColor = Console.ForegroundColor;
				if (originColor != color) Console.ForegroundColor = color;
				Console.WriteLine($"Tag${EPC}({State}) : {message}");
				Console.ForegroundColor = originColor;
			}
		}

		

		// Represent Specialized Channel Of The Environment
		public Channel Channel { get; private set; }

		private Mutex _mutex = new Mutex();

		// A designated RFID environment should have and only have 1 reader
		private readonly InterrogatorObject _interrogator;
		// A designated RFID environment should have >= 1 Tag(s)
		private readonly IList<TagObject> tagList = new List<TagObject>();

		public int TagCount => tagList.Count;
		/// <summary>
		/// Designated Only 1 Session In An Environment, This Flag Marked That Whether Reply (Tag -> Reader) Session Occupied
		/// </summary>
		private bool _isReplyOccupied = false;

		/// <summary>
		/// Designated Whether Replied In An Interaction
		/// </summary>
		private bool _isReplied = false;

		// Exclusive Constructor
		public Environment(Channel channel, InterrogatorObject interrogator, params TagObject[] tags)
		{
			Channel = channel;
			_interrogator = interrogator;
			_interrogator.SetEnvironment(this);
			foreach (var tag in tags)
			{
				tagList.Add(tag);
			}
		}

		/// <summary>
		/// ** Can Be Called By Reader Or Tag
		/// ** Used To Send Message To Designated Environment
		/// </summary>
		/// <param name="object"></param>
		/// <param name="message"></param>
		/// <exception cref="Exception"></exception>
		public void Send(Object @object, in byte[] message)
		{
			if (@object.GetType().IsSubclassOf(typeof(InterrogatorObject)))
			{
				Channel.CancelOccupy();
				var clonedMessage = message.Clone() as byte[];
				foreach (var tag in tagList)
				{
					Task.Run(() => tag.OnRequest(this, clonedMessage));
				}
				return;
			}

			_mutex.WaitOne();
			if (Channel.IsOccupied)
			{
				Thread.CurrentThread.Priority = ThreadPriority.Highest;
				_interrogator.OnConflict();// Might be called multiple times
				_mutex.ReleaseMutex();
				Thread.CurrentThread.Priority = ThreadPriority.Normal;
				return;
			}
			Channel.Occupy(SEND_FRAME_TIME);
			_mutex.ReleaseMutex();

			if (@object.GetType().IsSubclassOf(typeof(TagObject)))
			{
				var clonedMessage = message.Clone() as byte[];

				_interrogator.Receive(clonedMessage);

				return;
			}
			throw new Exception($"The First Argument Type `{@object.GetType()}` Must Be `{typeof(InterrogatorObject)}` Or `{typeof(TagObject)}`");
		}



	}
}
