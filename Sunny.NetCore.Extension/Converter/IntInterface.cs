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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.ValueSpan;
			if (str.Length != 8) throw new InvalidCastException();
			if (!TryParseInt(Unsafe.ReadUnaligned<long>(ref Unsafe.AsRef(in str.GetPinnableReference())), out var v)) throw new InvalidCastException();
			return v;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
		{
			var vector = IntToUtf8_8(value);
			writer.WriteStringValue(new ReadOnlySpan<byte>(&vector, 8));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool TryParseInt(string str, out int value)
		{
			long vector = default;
			System.Text.Encoding.UTF8.GetBytes(str, new Span<byte>(&vector, 8));
			return TryParseInt(vector, out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe string IntToString(int value)
		{
			var vector = IntToUtf8_8(value);
			return System.Text.Encoding.UTF8.GetString((byte*)&vector, 8);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe long IntToUtf8_8(int value)
		{
			var shuffle = Ssse3.Shuffle(*(Vector128<sbyte>*)&value, ShuffleMask);
			var vector = (((*(long*)&shuffle & HeightMask) >> 4) | ((*(long*)&shuffle & LowMask) << 8)) + ShortCharA;
			return vector;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseInt(long input, out int value)
		{
			value = default;
			var vector = input - ShortCharA;
			if ((vector & ShortN15) != 0) return false;
			vector = (vector << 4) | (vector >> 8);
			var output = Ssse3.Shuffle(*(Vector128<sbyte>*)&vector, NShuffleMask);
			value = *(int*)&output;
			return true;
		}
		private long ShortCharA = Unsafe.As<Vector256<short>, long>(ref GuidInterface.Singleton.ShortCharA);
		private long ShortN15 = Unsafe.As<Vector256<short>, long>(ref GuidInterface.Singleton.ShortN15);
		private long LowMask = Unsafe.As<Vector256<short>, long>(ref GuidInterface.Singleton.LowMask);
		private long HeightMask = Unsafe.As<Vector256<short>, long>(ref GuidInterface.Singleton.HeightMask);
		private readonly Vector128<sbyte> ShuffleMask = Vector128.Create(3, -1, 2, -1, 1, -1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0);
		private readonly Vector128<sbyte> NShuffleMask = Vector128.Create(6, 4, 2, 0, -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0);
	}
}
