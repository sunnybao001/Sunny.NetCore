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
	partial class DateFormat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe Vector128<byte> DateToUtf8_10(DateTime value)
		{
			var yyyy = value.Year;
			Vector128<int> numbers = default;   //最多4个值
			var nf = (int*)&numbers;
			nf[0] = yyyy / 100;
			nf[1] = yyyy - nf[0] * 100;
			nf[2] = value.Month;
			nf[3] = value.Day;
			Vector128<sbyte> vector = default;
			*(long*)&vector = NumberToUtf8Bit2(in numbers);
			*((byte*)&vector + 8) = (byte)'-';
			return Ssse3.Shuffle(vector, TUShuffleMask1).AsByte();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe Vector256<byte> DateTimeToUtf8_19(DateTime value)
		{
			var yyyy = value.Year;
			Vector256<int> numbers = default;   //最多8个值
			var nf = (int*)&numbers;
			nf[0] = yyyy / 100;
			nf[1] = yyyy - nf[0] * 100;
			nf[2] = value.Month;
			nf[3] = value.Day;
			nf[4] = value.Hour;
			nf[5] = value.Minute;
			nf[6] = value.Second;
			Vector256<sbyte> vector = default;
			*(Vector128<byte>*)&vector = NumberToUtf8Bit2(in numbers);
			*(short*)((byte*)&vector + 16) = *(short*)((byte*)&vector + 12);
			*((byte*)&vector + 12) = (byte)'-';
			*((byte*)&vector + 13) = (byte)' ';
			*((byte*)&vector + 14) = (byte)':';
			*((byte*)&vector + 18) = (byte)':';
			return Avx2.Shuffle(vector, TUShuffleMask).AsByte();
		}
		//最多输入4个数字，输出8个结果，每个数字最大值不能超过255。
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe long NumberToUtf8Bit2(in Vector128<int> numbers)
		{
			var vector = Sse2.And(Sse2.ShiftRightLogical(Sse2.MultiplyLow(Sse2.And(Sse2.Or(numbers, Sse2.ShiftLeftLogical(numbers, 16)).AsInt16(), SbyteMax1), ShortBit2X10Vector1), 7), SbyteMax1);
			var output = Sse2.PackUnsignedSaturate(Sse2.Add(Sse2.Subtract(vector, Sse2.MultiplyLow(Sse2.And(Sse2.ShiftRightLogical(Sse2.MultiplyLow(vector, ShortD1), 7), SbyteMax1), Short101)), ShortChar01), default);
			return *(long*)&output;
		}
		//最多输入8个数字，输出16个结果，每个数字最大值不能超过255。
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe Vector128<byte> NumberToUtf8Bit2(in Vector256<int> numbers)
		{
			var vector = Avx2.And(Avx2.ShiftRightLogical(Avx2.MultiplyLow(Avx2.And(Avx2.Or(numbers, Avx2.ShiftLeftLogical(numbers, 16)).AsInt16(), SbyteMax), ShortBit2X10Vector), 7), SbyteMax);
			vector = Avx2.Add(Avx2.Subtract(vector, Avx2.MultiplyLow(Avx2.And(Avx2.ShiftRightLogical(Avx2.MultiplyLow(vector, ShortD), 7), SbyteMax), Short10)), ShortChar0);
			var vectorf = (Vector128<short>*)&vector;
			return Sse2.PackUnsignedSaturate(vectorf[0], vectorf[1]);
		}
		private Vector256<short> Short10 = Vector256.Create((short)10);
		private Vector128<short> Short101;
		private Vector256<short> SbyteMax = Vector256.Create((short)sbyte.MaxValue);
		private Vector128<short> SbyteMax1;
		private Vector256<sbyte> TUShuffleMask = Vector256.Create(0, 1, 2, 3, 12, 4, 5, 12, 6, 7, 13, 8, 9, 14, 10, 11, 2, 0, 1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		private Vector128<sbyte> TUShuffleMask1 = Vector128.Create(0, 1, 2, 3, 8, 4, 5, 8, 6, 7, -1, -1, -1, -1, -1, -1);
	}
}
