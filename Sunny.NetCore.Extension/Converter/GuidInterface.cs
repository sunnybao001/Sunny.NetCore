using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public sealed class GuidInterface : System.Text.Json.Serialization.JsonConverter<Guid>
	{
		public static readonly GuidInterface Singleton = new GuidInterface();
		private GuidInterface() { }
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.ValueSpan;
			if (str.Length == 32)
			{
				if (TryParseGuid(in Unsafe.As<byte, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())), out var v)) return v;
			}
			else return reader.GetGuid();
			throw new InvalidCastException();
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
		{
			var vector = GuidToUtf8_32(in Unsafe.As<Guid, Vector128<byte>>(ref value));
			writer.WriteStringValue(new ReadOnlySpan<byte>(&vector, 32));
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe string GuidToString(ref Guid value)
		{
			var str = AsciiInterface.FastAllocateString(32);
			var vector = GuidToUtf8_32(in Unsafe.As<Guid, Vector128<byte>>(ref value)).AsByte();
			AsciiInterface.AsciiToUnicode(vector, ref Unsafe.As<char, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())));
			return str;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParse(string str, out Guid value)
		{
			var vector = AsciiInterface.UnicodeToAscii_32(ref Unsafe.As<char, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference()))).AsInt16();
			var r = TryParseGuid(in vector, out value);
			return r;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe Vector256<short> GuidToUtf8_32(in Vector128<byte> input)
		{
			var vector = Avx2.ConvertToVector256Int16(input);
			vector = Avx2.Add(Avx2.Or(Avx2.ShiftRightLogical(vector, 4), Avx2.ShiftLeftLogical(Avx2.And(vector, LowMask), 8)), ShortCharA);
			return vector;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseGuid(in Vector256<short> input, out Guid value)
		{
			var vector = Avx2.Subtract(input, ShortCharA);
			var r = Avx.TestZ(vector, ShortN15);
			vector = Avx2.And(Avx2.Or(Avx2.ShiftLeftLogical(vector, 4), Avx2.ShiftRightLogical(vector, 8)), FFMask);
			Unsafe.SkipInit(out value);
			Unsafe.As<Guid, Vector128<byte>>(ref value) = Sse2.PackUnsignedSaturate(Avx2.ExtractVector128(vector, 0), Avx2.ExtractVector128(vector, 1));
			return r;
		}
		//使用静态成员变量会造成CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
		internal Vector256<short> ShortCharA = Vector256.Create((sbyte)'A').AsInt16();
		internal Vector256<short> ShortN15 = Vector256.Create((sbyte)~15).AsInt16();
		internal Vector256<short> LowMask = Vector256.Create((short)15);
		internal Vector256<short> FFMask = Vector256.Create((short)0xFF);
		private readonly AsciiInterface AsciiInterface = AsciiInterface.Singleton;
	}
}
