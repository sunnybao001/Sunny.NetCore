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
	public sealed class GuidInterface : System.Text.Json.Serialization.JsonConverter<Guid>
	{
		public static readonly GuidInterface Singleton = new GuidInterface();
		private GuidInterface() { }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.ValueSpan;
			if (str.Length != 32) throw new InvalidCastException();
			if (!TryParseGuid(in Unsafe.As<byte, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())), out var v)) throw new InvalidCastException();
			return v;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
		{
			var vector = GuidToUtf8_32(in Unsafe.As<Guid, Vector128<byte>>(ref value));
			writer.WriteStringValue(new ReadOnlySpan<byte>(&vector, 32));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe string GuidToString(ref Guid value)
		{
			var vector = GuidToUtf8_32(in Unsafe.As<Guid, Vector128<byte>>(ref value)).AsByte();
			var f = stackalloc Vector256<short>[2];
			AsciiInterface.AsciiToUnicode(vector, f);
			return new string((char*)f, 0, 32);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool TryParseGuid(string str, out Guid value)
		{
			var vector = AsciiInterface.Singleton.UnicodeToAscii_32(ref Unsafe.As<char, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference()))).AsInt16();
			return TryParseGuid(in vector, out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe Vector256<short> GuidToUtf8_32(in Vector128<byte> input)
		{
			var vector = Avx2.ConvertToVector256Int16(input);
			vector = Avx2.Add(Avx2.Or(Avx2.ShiftRightLogical(vector, 4), Avx2.ShiftLeftLogical(Avx2.And(vector, LowMask), 8)), ShortCharA);
			return vector;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseGuid(in Vector256<short> input, out Guid value)
		{
			value = default;
			var vector = Avx2.Subtract(input, ShortCharA);	//很尴尬，JIT生成后的汇编会不断调用vmovupd将寄存器中的数据存入栈，再读取出来。
			if (!Avx.TestZ(vector, ShortN15)) return false;
			vector = Avx2.And(Avx2.Or(Avx2.ShiftLeftLogical(vector, 4), Avx2.ShiftRightLogical(vector, 8)), FFMask);
			var vectorf = (Vector128<short>*)&vector;
			Unsafe.As<Guid, Vector128<byte>>(ref value) = Sse2.PackUnsignedSaturate(vectorf[0], vectorf[1]);
			return true;
		}
		//使用静态成员变量造成CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
		internal Vector256<short> ShortCharA = Vector256.Create((sbyte)'A').AsInt16();
		internal Vector256<short> ShortN15 = Vector256.Create((sbyte)~15).AsInt16();
		internal Vector256<short> LowMask = Vector256.Create((short)15);
		internal Vector256<short> HeightMask = Vector256.Create((short)0xF0);
		internal Vector256<short> FFMask = Vector256.Create((short)0xFF);
	}
}
