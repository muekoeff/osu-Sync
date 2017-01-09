using System;
using System.IO;

namespace osuSync {
    public class OsuReader : BinaryReader {

		public OsuReader(Stream s) : base(s) {}

		public override string ReadString() {
			byte tag = ReadByte();
			if(tag == 0)
				return null;
			if(tag == 0xb)
				return base.ReadString();
			throw new IOException("Invalid string tag");
		}

		public DateTime ReadDate() {
			return new DateTime(ReadInt64());
		}
	}
}
