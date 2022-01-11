using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public class HexInterface : System.Text.Json.Serialization.JsonConverter<byte[]>
	{
		public static readonly HexInterface Singleton = new HexInterface();
		private HexInterface() { }
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
		{
			if (value.Length < 1024 * 128)
			{
				var buffer = stackalloc byte[value.Length * 2 + 2];
				Utf8Write(buffer, value);
				writer.WriteStringValue(new ReadOnlySpan<byte>(buffer, value.Length * 2 + 2));
			}
			else
			{
#if NET6_0_OR_GREATER
				var buffer = (byte*)System.Runtime.InteropServices.NativeMemory.Alloc((nuint)value.Length * 2 + 2);
#else
				var buffer = (byte*)System.Runtime.InteropServices.Marshal.AllocHGlobal(value.Length * 2 + 2);
#endif
				try
				{
					Utf8Write(buffer, value);
					writer.WriteStringValue(new ReadOnlySpan<byte>(buffer, value.Length * 2 + 2));
				}
				finally
				{
#if NET6_0_OR_GREATER
					System.Runtime.InteropServices.NativeMemory.Free(buffer);
#else
					System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)buffer);
#endif
				}
			}
		}
		private unsafe void Utf8Write(byte* buffer, byte[] value)
		{
			*buffer = (byte)'0';
			buffer[1] = (byte)'x';
			buffer += 2;
			int offset = 0;
			for (; offset + 16 <= value.Length; offset += 16)
			{
				*(Vector256<byte>*)buffer = Byte16ToUtf8_32(Unsafe.As<byte, Vector128<byte>>(ref value[offset])).AsByte();
				buffer += 16 * 2;
			}
			for (; offset + 8 <= value.Length; offset += 8)
			{
				*(Vector128<byte>*)buffer = Byte8ToUtf8_32(Unsafe.As<byte, Vector128<byte>>(ref value[offset])).AsByte();
				buffer += 8 * 2;
			}
			if (offset < value.Length)
			{
				var buffer1 = stackalloc byte[8 * 2];
				*(Vector128<byte>*)buffer1 = Byte8ToUtf8_32(Unsafe.As<byte, Vector128<byte>>(ref value[offset])).AsByte();
				Unsafe.CopyBlock(buffer, buffer1, (uint)((value.Length - offset) * 2 * 2));
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe string BytesToString(byte[] value, bool prefix = false)
		{
#if NET6_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(value);
#else
			if (value == null) throw new ArgumentNullException(nameof(value));
#endif
			var str = AsciiInterface.FastAllocateString(value.Length * 2 + (prefix ? 2 : 0));
			ref char strSite = ref Unsafe.AsRef(in str.GetPinnableReference());
			if (prefix)
			{
				strSite = '0';
				Unsafe.Add(ref strSite, 1) = 'x';
				strSite = ref Unsafe.Add(ref strSite, 2);
			}
			int offset = 0;
			for (; offset + 16 <= value.Length; offset += 16)
			{
				var vector = Byte16ToUtf8_32(Unsafe.As<byte, Vector128<byte>>(ref value[offset])).AsByte();
				AsciiInterface.AsciiToUnicode(vector, ref Unsafe.As<char, Vector256<short>>(ref strSite));
				strSite = ref Unsafe.Add(ref strSite, 16 * 2);
			}
			for (; offset + 8 <= value.Length; offset += 8)
			{
				var vector = Byte8ToUtf8_32(Unsafe.As<byte, Vector128<byte>>(ref value[offset])).AsByte();
				AsciiInterface.AsciiToUnicode(vector, ref Unsafe.As<char, Vector256<short>>(ref strSite));
				strSite = ref Unsafe.Add(ref strSite, 8 * 2);
			}
			if (offset < value.Length)
			{
				var buffer = stackalloc char[8 * 2];
				var vector = Byte8ToUtf8_32(Unsafe.As<byte, Vector128<byte>>(ref value[offset])).AsByte();
				AsciiInterface.AsciiToUnicode(vector, ref *(Vector256<short>*)buffer);
				Unsafe.CopyBlock(ref Unsafe.As<char, byte>(ref strSite), ref *(byte*)buffer, (uint)((value.Length - offset) * 2 * 2));
			}
			return str;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe Vector256<sbyte> Byte16ToUtf8_32(Vector128<byte> input)
		{
			var vector = Avx2.ConvertToVector256Int16(input);
			var byteNum = Avx2.Or(Avx2.ShiftRightLogical(vector, 4), Avx2.ShiftLeftLogical(Avx2.And(vector, LowMask), 8)).AsSByte();
			var gv = Avx2.CompareGreaterThan(byteNum, sb9); //>9
			var r = Avx2.Add(Avx2.Or(Avx2.AndNot(gv, sb0), Avx2.And(gv, sba)), byteNum);
			return r;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe Vector128<sbyte> Byte8ToUtf8_32(Vector128<byte> input)
		{
			var vector = Sse41.ConvertToVector128Int16(input);
			var byteNum = Sse2.Or(Sse2.ShiftRightLogical(vector, 4), Sse2.ShiftLeftLogical(Sse2.And(vector, Unsafe.As<Vector256<short>, Vector128<short>>(ref LowMask)), 8)).AsSByte();
			var gv = Sse2.CompareGreaterThan(byteNum, Unsafe.As<Vector256<sbyte>, Vector128<sbyte>>(ref sb9)); //>9
			var r = Sse2.Add(Sse2.Or(Sse2.AndNot(gv, Unsafe.As<Vector256<sbyte>, Vector128<sbyte>>(ref sb0)), Sse2.And(gv, Unsafe.As<Vector256<sbyte>, Vector128<sbyte>>(ref sba))), byteNum);
			return r;
		}

		private Vector256<sbyte> sb9 = Vector256.Create((sbyte)9);
		private Vector256<sbyte> sb0 = Vector256.Create((sbyte)'0');
		private Vector256<sbyte> sba = Vector256.Create((sbyte)(((sbyte)'a')-10));
		private Vector256<short> LowMask = GuidInterface.Singleton.LowMask;
		private readonly AsciiInterface AsciiInterface = AsciiInterface.Singleton;
	}
}
