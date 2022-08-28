using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace ArrangeByPenis
{
	[Serializable()]
	class IconInfo : ISerializable
	{

		public string name;
		public int x;
		public int y;

		public IconInfo(string n, int _x, int _y)
		{
			name = n;
			x = _x;
			y = _y;
		}

		public IconInfo(SerializationInfo info, StreamingContext ctxt)
		{
			name = (string)info.GetValue("name", typeof(string));
			x = (int)info.GetValue("x", typeof(int));
			y = (int)info.GetValue("y", typeof(int));
		}
 
		public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
		{
			info.AddValue("name", name);
			info.AddValue("x", x);
			info.AddValue("y", y);
		}
	}
}
