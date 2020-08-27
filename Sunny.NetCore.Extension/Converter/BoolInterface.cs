using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public sealed class BoolInterface : System.Text.Json.Serialization.JsonConverter<bool>
	{
		public static readonly BoolInterface Singleton = new BoolInterface();
		private BoolInterface() { }
		public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var v = reader.GetInt32();
			return v != 0;
		}
		public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
		{
			writer.WriteNumberValue(value ? 1 : 0);
		}
	}
}
