using System;
using RFID.Tags;
using RFID.Readers;
namespace RFID
{
	class Program
	{
		static void Main(string[] args)
		{
			var tag1 = new Tag(1).PowerOn();
			var tag2 = new Tag(2).PowerOn();

			var reader = new Reader();

			var env = new Environment(reader, tag1, tag2);

			reader.Start();
			
		}
	}
}
