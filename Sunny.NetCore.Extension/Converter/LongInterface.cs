using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public sealed class LongInterface : System.Text.Json.Serialization.JsonConverter<long>
	{
		public static readonly LongInterface Singleton = new LongInterface();
		private LongInterface() { }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.ValueSpan;
			if (str.Length != 16) throw new InvalidCastException();
			if (!TryParseLong(in Unsafe.As<byte, Vector128<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())), out var v)) throw new InvalidCastException();
			return v;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
		{
			var vector = LongToUtf8_16(value);
			writer.WriteStringValue(new ReadOnlySpan<byte>(&vector, 16));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool TryParseLong(string str, out long value)
		{
			var vector = AsciiInterface.Singleton.UnicodeToAscii_16(in Unsafe.As<char, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference()))).AsInt16();
			return TryParseLong(in vector, out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe string LongToString(long value)
		{
			var vector = Avx2.ConvertToVector256Int16(LongToUtf8_16(value));
			return new string((char*)&vector, 0, 16);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe Vector128<byte> LongToUtf8_16(long value)
		{
			var vector = Ssse3.Shuffle(*(Vector128<sbyte>*)&value, ShuffleMask).AsInt16();
			return Sse2.Add(Sse2.Or(Sse2.ShiftRightLogical(vector, 4), Sse2.ShiftLeftLogical(Sse2.And(vector, LowMask), 8)), ShortCharA).AsByte();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseLong(in Vector128<short> input, out long value)
		{
			value = default;
			var vector = Sse2.Subtract(input, ShortCharA);
			if (!Sse41.TestZ(vector, ShortN15)) return false;
			value = Sse41.X64.Extract(Ssse3.Shuffle(Sse2.Or(Sse2.ShiftLeftLogical(vector, 4), Sse2.ShiftRightLogical(vector, 8)).AsSByte(), NShuffleMask).AsInt64(), 0);
			return true;
		}
		private Vector128<short> ShortCharA = Unsafe.As<Vector256<short>, Vector128<short>>(ref GuidInterface.Singleton.ShortCharA);
		private Vector128<short> ShortN15 = Unsafe.As<Vector256<short>, Vector128<short>>(ref GuidInterface.Singleton.ShortN15);
		private Vector128<short> LowMask = Unsafe.As<Vector256<short>, Vector128<short>>(ref GuidInterface.Singleton.LowMask);
		private Vector128<short> HeightMask = Unsafe.As<Vector256<short>, Vector128<short>>(ref GuidInterface.Singleton.HeightMask);
		private readonly Vector128<sbyte> ShuffleMask = Vector128.Create(7, -1, 6, -1, 5, -1, 4, -1, 3, -1, 2, -1, 1, -1, 0, -1);
		private readonly Vector128<sbyte> NShuffleMask = Vector128.Create(14, 12, 10, 8, 6, 4, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0);
	}
}
