using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public sealed class LongInterface : System.Text.Json.Serialization.JsonConverter<long>
	{
		public static readonly LongInterface Singleton;
		internal LongInterface() { }
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number) return reader.GetInt64();
			var str = reader.ValueSpan;
			if (str.Length != 16) throw new InvalidCastException();
			if (TryParseLong(in Unsafe.As<byte, Vector128<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())), out var v)) return v;
			throw new InvalidCastException();
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
		{
			var vector = LongToUtf8_16(value);
			writer.WriteStringValue(new ReadOnlySpan<byte>(&vector, 16));
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParse(string str, out long value)
		{
			var vector = AsciiInterface.Singleton.UnicodeToAscii_16(in Unsafe.As<char, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference()))).AsInt16();
			return TryParseLong(in vector, out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe string LongToString(long value)
		{
			var vector = Avx2.ConvertToVector256Int16(LongToUtf8_16(value));
			return new string((char*)&vector, 0, 16);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe Vector128<byte> LongToUtf8_16(long value)
		{
			return Sse41.X64.IsSupported ? LongToUtf8_16X64(value) : LongToUtf8_16X86(value);	//会在JIT时进行静态判断
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private Vector128<byte> LongToUtf8_16X64(long value)
		{
			var vector = Ssse3.Shuffle(Sse41.X64.Insert(default, value, 0).AsSByte(), ShuffleMask).AsInt16();
			return Sse2.Add(Sse2.Or(Sse2.ShiftRightLogical(vector, 4), Sse2.ShiftLeftLogical(Sse2.And(vector, LowMask), 8)), ShortCharA).AsByte();
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe Vector128<byte> LongToUtf8_16X86(long value)
		{
			var vector = Ssse3.Shuffle(*(Vector128<sbyte>*)&value, ShuffleMask).AsInt16();
			return Sse2.Add(Sse2.Or(Sse2.ShiftRightLogical(vector, 4), Sse2.ShiftLeftLogical(Sse2.And(vector, LowMask), 8)), ShortCharA).AsByte();
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private bool TryParseLong(in Vector128<short> input, out long value)
		{
			return Sse41.X64.IsSupported ? TryParseLongX64(in input, out value) : TryParseLongX86(in input, out value);	//会在JIT时进行静态判断
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private bool TryParseLongX64(in Vector128<short> input, out long value)
		{
			var vector = Sse2.Subtract(input, ShortCharA);
			var r = Sse41.TestZ(vector, ShortN15);
			value = Sse41.X64.Extract(Ssse3.Shuffle(Sse2.Or(Sse2.ShiftLeftLogical(vector, 4), Sse2.ShiftRightLogical(vector, 8)).AsSByte(), NShuffleMask).AsInt64(), 0);
			return r;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe bool TryParseLongX86(in Vector128<short> input, out long value)
		{
			var vector = Sse2.Subtract(input, ShortCharA);
			var r = Sse41.TestZ(vector, ShortN15);
			vector = Ssse3.Shuffle(Sse2.Or(Sse2.ShiftLeftLogical(vector, 4), Sse2.ShiftRightLogical(vector, 8)).AsSByte(), NShuffleMask).AsInt16();
			value = *(long*)&vector;
			return r;
		}
		static LongInterface()
		{
			Singleton = new LongInterface();
		}
		internal readonly Vector128<short> ShortCharA = Avx2.ExtractVector128(GuidInterface.Singleton.ShortCharA, 0);
		internal Vector128<short> ShortN15 = Avx2.ExtractVector128(GuidInterface.Singleton.ShortN15, 0);
		internal Vector128<short> LowMask = Avx2.ExtractVector128(GuidInterface.Singleton.LowMask, 0);
		internal Vector128<short> HeightMask = Vector128.Create((short)0xF0);
		private readonly Vector128<sbyte> ShuffleMask = Vector128.Create(7, -1, 6, -1, 5, -1, 4, -1, 3, -1, 2, -1, 1, -1, 0, -1);
		internal readonly Vector128<sbyte> NShuffleMask = Vector128.Create(14, 12, 10, 8, 6, 4, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0);
	}
}
