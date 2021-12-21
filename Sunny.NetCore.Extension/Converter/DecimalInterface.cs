
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public sealed class DecimalInterface : System.Text.Json.Serialization.JsonConverter<decimal>
	{
		public static readonly DecimalInterface Singleton = new DecimalInterface();
		private DecimalInterface() { }
		public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				if (!System.Buffers.Text.Utf8Parser.TryParse(reader.ValueSpan, out decimal r, out _)) throw new JsonException();
				return r;
			}
			return reader.GetDecimal();
		}

		public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
		{
			writer.WriteNumberValue(value);
		}
	}
}
