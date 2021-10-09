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
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var s = reader.ValueSpan;
			if (TryParseDateTime(s, out var value)) return value;
			throw new ArgumentException("DateTime的格式不正确");
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe string DateTimeToString(DateTime value)
		{
			if (value.TimeOfDay.Ticks == default)
			{
				var str = AsciiInterface.FastAllocateString(10);
				var vector = Avx2.ConvertToVector256Int16(DateToUtf8_10(value));
				AsciiInterface.StringTo<char, Vector128<short>>(str) = vector.GetLower();
				Unsafe.Add(ref AsciiInterface.StringTo<char, int>(str), 4) = vector.AsInt32().GetElement(4);
				return str;
			}
			else
			{
				var str = AsciiInterface.FastAllocateString(19);
				var r = DateTimeToUtf8_19(value);
				Set38(Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(r, 0)), Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(r, 1)), ref Unsafe.AsRef(in str.GetPinnableReference()));
				return str;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private static void Set38(Vector256<short> vector0, Vector256<short> vector1, ref char output)
		{
			Unsafe.As<char, Vector256<short>>(ref output) = vector0;
			Unsafe.Add(ref Unsafe.As<char, int>(ref output), 8) = vector1.AsInt32().GetElement(0);
			Unsafe.Add(ref Unsafe.As<char, short>(ref output), 18) = vector1.GetElement(2);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParseDateTime(string str, out DateTime value)
		{
			var vector = AsciiInterface.UnicodeToAscii_32(ref AsciiInterface.StringTo<char, Vector256<short>>(str));
			return TryParseDateTime(new ReadOnlySpan<byte>(&vector, str.Length), out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseDateTime(ReadOnlySpan<byte> input, out DateTime value)
		{
			Unsafe.SkipInit(out value);
			bool success = true;
			if (input.Length == 19 | input.Length == 20) return Utf8_19ToDate(in AsciiInterface.StringTo<byte, Vector256<sbyte>>(input), out value);
			if (input.Length == 10 | input.Length == 11)
			{
				var v128 = AsciiInterface.StringTo<byte, Vector128<sbyte>>(input);
				if (IsNumber(v128, input.Length))
				{
					System.Buffers.Text.Utf8Parser.TryParse(input, out long lv, out _);
					value = new DateTime(1970, 1, 1).AddTicks(TimeSpan.TicksPerSecond * lv);
					return true;
				}
				return Utf8_10ToDate(in v128, out value);
			}
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
					return false;
				}
				value = new DateTime(year, month, day);
				return true;
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private bool IsNumber(Vector128<sbyte> input, int length)
		{
			var success = Sse2.MoveMask(Sse2.CompareEqual(Sse2.And(Sse2.Subtract(input, SByteChat01), SByteN15), Vector128<sbyte>.Zero));
			//这里本来应该用TZCNT，但.NET不支持
			var test = (1 << length) - 1;
			return test == (success & test);
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
		private readonly Vector128<sbyte> SByteChat01 = Vector128.Create((sbyte)'0');
		private readonly Vector128<sbyte> SByteN15 = Vector128.Create((sbyte)~15);
		private readonly AsciiInterface AsciiInterface = AsciiInterface.Singleton;
	}
}
