using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public class HexInterface
	{
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe string BytesToString(byte[] value)
		{
			ArgumentNullException.ThrowIfNull(value);
			var str = AsciiInterface.FastAllocateString(value.Length * 2 + 2);
			ref char strSite = ref Unsafe.AsRef(in str.GetPinnableReference());
			strSite = '0';
			Unsafe.Add(ref strSite, 1) = 'x';
			strSite = ref Unsafe.Add(ref strSite, 2);
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
			var byteNum = Avx2.Or(Avx2.ShiftRightLogical(vector, 4), Avx2.ShiftLeftLogical(Avx2.And(vector, GuidInterface.LowMask), 8)).AsSByte();
			var gv = Avx2.CompareGreaterThan(byteNum, sb9); //>9
			var r = Avx2.Add(Avx2.Or(Avx2.AndNot(gv, sb0), Avx2.And(gv, sba)), byteNum);
			return r;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe Vector128<sbyte> Byte8ToUtf8_32(Vector128<byte> input)
		{
			var vector = Sse41.ConvertToVector128Int16(input);
			var byteNum = Sse2.Or(Sse2.ShiftRightLogical(vector, 4), Sse2.ShiftLeftLogical(Sse2.And(vector, LongInterface.LowMask), 8)).AsSByte();
			var gv = Sse2.CompareGreaterThan(byteNum, Unsafe.As<Vector256<sbyte>, Vector128<sbyte>>(ref sb9)); //>9
			var r = Sse2.Add(Sse2.Or(Sse2.AndNot(gv, Unsafe.As<Vector256<sbyte>, Vector128<sbyte>>(ref sb0)), Sse2.And(gv, Unsafe.As<Vector256<sbyte>, Vector128<sbyte>>(ref sba))), byteNum);
			return r;
		}
		private Vector256<sbyte> sb9 = Vector256.Create((sbyte)9);
		private Vector256<sbyte> sb0 = Vector256.Create((sbyte)'0');
		private Vector256<sbyte> sba = Vector256.Create((sbyte)(((sbyte)'a')-10));
		private readonly AsciiInterface AsciiInterface = AsciiInterface.Singleton;
		private readonly GuidInterface GuidInterface = GuidInterface.Singleton;
		private readonly LongInterface LongInterface = LongInterface.Singleton;
	}
}
