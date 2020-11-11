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
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.ValueSpan;
			if (str.Length != 8) throw new InvalidCastException();
			if (!TryParseInt(Unsafe.ReadUnaligned<long>(ref Unsafe.AsRef(in str.GetPinnableReference())), out var v)) throw new InvalidCastException();
			return v;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
		{
			var vector = IntToUtf8_8(value);
			writer.WriteStringValue(new ReadOnlySpan<byte>(&vector, 8));
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParseInt(string str, out int value)
		{
			long vector;
			Encoding.UTF8.GetBytes(str, new Span<byte>(&vector, 8));
			return TryParseInt(vector, out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe string IntToString(int value)
		{
			var str = AsciiInterface.FastAllocateString(8);
			var vector = IntToUtf8_8(value);
			Unsafe.As<char, Vector128<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())) = Sse41.ConvertToVector128Int16((byte*)&vector);
			return str;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe long IntToUtf8_8(int value)
		{
			var shuffle = Extract64(Ssse3.Shuffle(Vector128.CreateScalarUnsafe(value).AsSByte(), ShuffleMask));
			return (((shuffle & HeightMask) >> 4) | ((shuffle & LowMask) << 8)) + ShortCharA;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseInt(long input, out int value)
		{
			var vector = input - ShortCharA;
			var r = (vector & ShortN15) == 0;
			vector = (long)((((ulong)vector) << 4) | (((ulong)vector) >> 8));
			value = Sse41.Extract(Ssse3.Shuffle(Vector128.CreateScalar(vector).AsSByte(), NShuffleMask).AsInt32(), 0);
			return r;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		internal static unsafe long Extract64(Vector128<sbyte> value)
		{
			if (Sse41.X64.IsSupported) return Sse41.X64.Extract(value.AsInt64(), 0); //会在JIT时进行静态判断
			var v = value.AsInt32();
			return (long)((uint)Sse41.Extract(v, 0) | ((ulong)Sse41.Extract(v, 1) << 32));
		}
		private long ShortCharA = Extract64(LongInterface.Singleton.ShortCharA.AsSByte());
		private long ShortN15 = Extract64(LongInterface.Singleton.ShortN15.AsSByte());
		private long LowMask = Extract64(LongInterface.Singleton.LowMask.AsSByte());
		private long HeightMask = Extract64(LongInterface.Singleton.HeightMask.AsSByte());
		private readonly Vector128<sbyte> ShuffleMask = Vector128.Create(3, -1, 2, -1, 1, -1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0);
		private readonly Vector128<sbyte> NShuffleMask = Vector128.Create(6, 4, 2, 0, -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0);
		private AsciiInterface AsciiInterface = AsciiInterface.Singleton;
	}
}
