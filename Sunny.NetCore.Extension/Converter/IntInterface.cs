using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;

namespace Sunny.NetCore.Extension.Converter
{
	public sealed class IntInterface : System.Text.Json.Serialization.JsonConverter<int>
	{
		public static readonly IntInterface Singleton = new IntInterface();
		private IntInterface() { }
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public override unsafe int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.ValueSpan;
			if (str.Length != 8) throw new InvalidCastException();
			if (!TryParseInt(Unsafe.ReadUnaligned<long>(ref Unsafe.AsRef(in str.GetPinnableReference())), out var v)) throw new InvalidCastException();
			return v;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public override unsafe void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
		{
			var vector = IntToUtf8_8(value);
			writer.WriteStringValue(new ReadOnlySpan<byte>(&vector, 8));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParseInt(string str, out int value)
		{
			long vector = default;
			Encoding.UTF8.GetBytes(str, new Span<byte>(&vector, 8));
			return TryParseInt(vector, out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public unsafe string IntToString(int value)
		{
			var vector = IntToUtf8_8(value);
			var str = Sse41.ConvertToVector128Int16((byte*)&vector);
			return new string((char*)&str, 0, 8);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private unsafe long IntToUtf8_8(int value)
		{
			var shuffle = Ssse3.Shuffle(*(Vector128<sbyte>*)&value, ShuffleMask);
			return (((*(long*)&shuffle & HeightMask) >> 4) | ((*(long*)&shuffle & LowMask) << 8)) + ShortCharA;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private unsafe bool TryParseInt(long input, out int value)
		{
			var vector = input - ShortCharA;
			var r = (vector & ShortN15) != 0;
			vector = (vector << 4) | (vector >> 8);
			value = Sse41.Extract(Ssse3.Shuffle(*(Vector128<sbyte>*)&vector, NShuffleMask).AsInt32(), 0);
			return r;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static long Extract64(Vector128<short> value)
		{
			if (Sse41.X64.IsSupported) return Sse41.X64.Extract(value.AsInt64(), 0);
			var v = value.AsInt32();
#pragma warning disable CS0675 // 对进行了带符号扩展的操作数使用了按位或运算符
			return Sse41.Extract(v, 0) | ((long)Sse41.Extract(v, 1) << 32);
#pragma warning restore CS0675 // 对进行了带符号扩展的操作数使用了按位或运算符
		}
		private long ShortCharA = Extract64(LongInterface.Singleton.ShortCharA);
		private long ShortN15 = Extract64(LongInterface.Singleton.ShortN15);
		private long LowMask = Extract64(LongInterface.Singleton.LowMask);
		private long HeightMask = Extract64(LongInterface.Singleton.HeightMask);
		private readonly Vector128<sbyte> ShuffleMask = Vector128.Create(3, -1, 2, -1, 1, -1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0);
		private readonly Vector128<sbyte> NShuffleMask = Vector128.Create(6, 4, 2, 0, -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0);
	}
}
