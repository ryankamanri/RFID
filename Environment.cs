using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RFID
{
	public class Environment
	{
		public class Object
		{
			/// <summary>
			/// The Buffer That Every Object Has
			/// </summary>
			public byte[] Buffer { get; set; }
		}
		
		public abstract class InterrogatorObject : Object
		{
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
		}
		
		public abstract class TagObject : Object
		{
			/// <summary>
			/// Set Handler While Tag Receiving Request From Reader
			/// </summary>
			/// <param name="request"></param>
			public abstract void OnRequest(Environment environment, in byte[] request);
		}

		// A designated RFID environment should have and only have 1 reader
		private readonly InterrogatorObject _interrogator;
		// A designated RFID environment should have >= 1 Tag(s)
		private readonly IList<TagObject> tagList = new List<TagObject>();
		/// <summary>
		/// Designated Only 1 Session In An Environment, This Flag Marked That Whether Reply (Tag -> Reader) Session Occupied
		/// </summary>
		private bool _isReplyOccupied = false;

		/// <summary>
		/// Designated Whether Replied In An Interaction
		/// </summary>
		private bool _isReplied = false;

		// Exclusive Constructor
		public Environment(InterrogatorObject interrogator, params TagObject[] tags)
		{
			this._interrogator = interrogator;
			this._interrogator.SetEnvironment(this);
			foreach(var tag in tags)
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
				_isReplyOccupied = false;
				_isReplied = false;
				var clonedMessage = message.Clone() as byte[];
				foreach (var tag in tagList)
				{
					Task.Run(() => tag.OnRequest(this, clonedMessage));
				}
				return;
			}
			if (_isReplied) return;
			if (_isReplyOccupied)
			{
				_isReplied = true;
				_interrogator.OnConflict();// Might be called multiple times
				return;
			}
			if (@object.GetType().IsSubclassOf(typeof(TagObject)))
			{
				_isReplyOccupied = true;
				_interrogator.Receive(message);
				_isReplied = true;
				return;
			}
			throw new Exception($"The First Argument Type `{@object.GetType()}` Must Be `{typeof(InterrogatorObject)}` Or `{typeof(TagObject)}`");
		}

		

	}
}
