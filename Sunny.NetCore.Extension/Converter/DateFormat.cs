using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public sealed partial class DateFormat : System.Text.Json.Serialization.JsonConverter<DateTime>
	{
		public static readonly DateFormat Singleton;
		internal DateFormat() {
			ShortChar01 = Avx2.ExtractVector128(ShortChar0, 0);
			ShortD1 = Avx2.ExtractVector128(ShortD, 0);
			ShortBit2X10Vector1 = Avx2.ExtractVector128(ShortBit2X10Vector, 0);
			Int101 = Avx2.ExtractVector128(Int10, 0);
			Short101 = Avx2.ExtractVector128(Short10, 0);
			SbyteMax1 = Avx2.ExtractVector128(SbyteMax, 0);
			ShortN151 = Avx2.ExtractVector128(ShortN15, 0);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public override unsafe DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var s = reader.ValueSpan;
			if (TryParseDateTime(s, out var value)) return value;
			throw new ArgumentException("DateTime的格式不正确");
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public override unsafe void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			if (value.TimeOfDay.Ticks == default)
			{
				var r = DateToUtf8_10(value);
				writer.WriteStringValue(new ReadOnlySpan<byte>(&r, 10));
			}
			else
			{
				var r = DateTimeToUtf8_19(value);
				writer.WriteStringValue(new ReadOnlySpan<byte>(&r, 19));
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public unsafe string DateTimeToString(DateTime value)
		{
			if (value.TimeOfDay.Ticks == default)
			{
				var vector = Avx2.ConvertToVector256Int16(DateToUtf8_10(value));
				return new string((char*)&vector, 0, 10);
			}
			else
			{
				var r = DateTimeToUtf8_19(value);
				var f = stackalloc Vector256<short>[2];
				AsciiInterface.AsciiToUnicode(r, f);
				return new string((char*)f, 0, 19);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParseDateTime(string str, out DateTime value)
		{
			var vector = AsciiInterface.Singleton.UnicodeToAscii_32(ref Unsafe.As<char, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())));
			return TryParseDateTime(new ReadOnlySpan<byte>(&vector, str.Length), out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private unsafe bool TryParseDateTime(ReadOnlySpan<byte> input, out DateTime value)
		{
			bool success = true;
			if (input.Length == 19 | input.Length == 20) return Utf8_19ToDate(in Unsafe.As<byte, Vector256<sbyte>>(ref Unsafe.AsRef(in input.GetPinnableReference())), out value);
			if (input.Length == 10) return Utf8_10ToDate(in Unsafe.As<byte, Vector128<sbyte>>(ref Unsafe.AsRef(in input.GetPinnableReference())), out value);
			if (input.Length < 10 & input.Length > 7)
			{
				int start = 0, length = 4;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int year, out _);
				start += length + 1;
				length = 0;
				while (length != 2 & input[start + length] != '-') ++length;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int month, out _);
				start += length + 1;
				length = 0;
				while ((start + length != input.Length) && (length != 2 & input[start + length] != '-')) ++length;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int day, out _);
				if (!success)
				{
					value = default;
					return false;
				}
				value = new DateTime(year, month, day);
				return true;
			}
			value = default;
			return false;
		}
		static DateFormat()
		{
			Singleton = new DateFormat();
		}
		private Vector256<short> ShortChar0 = Vector256.Create((short)'0');
		private Vector256<short> ShortD = Vector256.Create((short)0xD);   //D CD CCCD
																				 //7 11 19 分别最大值255 2047 524287
																				 //最大化精度需要容器：short int long
		private Vector256<short> ShortBit2X10Vector = Vector256.Create(0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80);
		internal readonly Vector128<short> ShortChar01;
		internal readonly Vector128<short> ShortD1;
		internal readonly Vector128<short> ShortBit2X10Vector1;
	}
}
