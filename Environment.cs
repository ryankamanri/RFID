using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RFID
{
	public class Environment
	{
		public class Object
		{
			public Environment Environment { get; private set; }
			public byte[] Buffer { get; set; }

			public void SetEnvironment(Environment env) => Environment = env;
		}
		
		public abstract class ReaderObject : Object
		{
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
			/// <param name="require"></param>
			public abstract void OnRequest(in byte[] require);
		}

		// A designated RFID environment should have and only have 1 reader
		private readonly ReaderObject reader;
		// A designated RFID environment should have >= 1 Tag(s)
		private readonly IList<TagObject> tagList = new List<TagObject>();

		private bool isReplyOccupied = false;

		// Exclusive Constructor
		public Environment(ReaderObject reader, params TagObject[] tags)
		{
			this.reader = reader;
			this.reader.SetEnvironment(this);
			foreach(var tag in tags)
			{
				tag.SetEnvironment(this);
				tagList.Add(tag);
			}
		}
		
		public void Send(Object @object, in byte[] message)
		{
			if (@object.GetType().IsSubclassOf(typeof(ReaderObject)))
			{
				isReplyOccupied = false;
				var clonedMessage = message.Clone() as byte[];
				foreach (var tag in tagList)
				{
					Task.Run(() => tag.OnRequest(clonedMessage));
				}
				return;
			}
			if (isReplyOccupied)
			{
				reader.OnConflict();// Might be called multiple times
				return;
			}
			if (@object.GetType().IsSubclassOf(typeof(TagObject)))
			{
				isReplyOccupied = true;
				reader.Receive(message);
				return;
			}
			throw new Exception($"The First Argument Type `{@object.GetType()}` Must Be `{typeof(ReaderObject)}` Or `{typeof(TagObject)}`");
		}

		

	}
}
