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
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe Vector128<byte> DateToUtf8_10(DateTime value)
		{
			var yyyy = value.Year;
			var yt = yyyy / 100;
			var MM = value.Month;
			var dd = value.Day;
			var numbers = Vector128.Create(yt, yyyy - yt * 100, MM, dd); //JIT内部优化
			var v = Ssse3.Shuffle(Sse41.Insert(NumberToUtf8Bit2(numbers), (sbyte)'-', 8), TUShuffleMask1).AsByte();	//寄存器优化
			return v;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe Vector256<byte> DateTimeToUtf8_19(DateTime value)
		{
			var yyyy = value.Year;
			var yt = yyyy / 100;
			var MM = value.Month;
			var dd = value.Day;
			var hh = value.Hour;
			var mm = value.Minute;
			var ss = value.Second;
			Vector256<int> numbers = Vector256.Create(yt, yyyy - yt * 100, MM, dd, hh, mm, ss, yt);	//JIT内部优化
			var v128 = NumberToUtf8Bit2(numbers);
			Vector256<sbyte> vector = Vector256.Create(
				Sse41.Insert(Sse41.Insert(Sse41.Insert(v128, (sbyte)'-', 12), (sbyte)' ', 13), (sbyte)':', 14),
				Sse41.Insert(Sse2.Insert(v128.AsUInt16(), Sse2.Extract(v128.AsUInt16(), 6), 0).AsSByte(), (sbyte)':', 2));
			var v = Avx2.Shuffle(vector, TUShuffleMask).AsByte();
			return v;
		}
		//最多输入4个数字，输出8个结果，每个数字最大值不能超过255。
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private Vector128<sbyte> NumberToUtf8Bit2(Vector128<int> numbers)
		{
			var vector = Sse2.And(Sse2.ShiftRightLogical(Sse2.MultiplyLow(Sse2.And(Sse2.Or(Sse2.ShiftLeftLogical(numbers, 16), numbers).AsInt16(), this.SbyteMax1), this.ShortBit2X10Vector1), 7), this.SbyteMax1);
			vector = Sse2.Add(Sse2.Subtract(vector, Sse2.MultiplyLow(Sse2.And(Sse2.ShiftRightLogical(Sse2.MultiplyLow(vector, ShortD1), 7), this.SbyteMax1), this.Short101)), this.ShortChar01);
			return Sse2.PackSignedSaturate(vector, vector);
		}
		//最多输入8个数字，输出16个结果，每个数字最大值不能超过255。
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private Vector128<sbyte> NumberToUtf8Bit2(Vector256<int> numbers)
		{
			var vector = Avx2.And(Avx2.ShiftRightLogical(Avx2.MultiplyLow(Avx2.And(Avx2.Or(numbers, Avx2.ShiftLeftLogical(numbers, 16)).AsInt16(), SbyteMax), ShortBit2X10Vector), 7), SbyteMax);
			vector = Avx2.Add(Avx2.Subtract(vector, Avx2.MultiplyLow(Avx2.And(Avx2.ShiftRightLogical(Avx2.MultiplyLow(vector, ShortD), 7), SbyteMax), Short10)), ShortChar0);
			return Sse2.PackSignedSaturate(Avx2.ExtractVector128(vector, 0), Avx2.ExtractVector128(vector, 1));
		}
		private Vector256<short> Short10 = Vector256.Create((short)10);
		internal readonly Vector128<short> Short101;
		private Vector256<short> SbyteMax = Vector256.Create((short)sbyte.MaxValue);
		internal readonly Vector128<short> SbyteMax1;
		private Vector256<sbyte> TUShuffleMask = Vector256.Create(0, 1, 2, 3, 12, 4, 5, 12, 6, 7, 13, 8, 9, 14, 10, 11, 2, 0, 1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		private Vector128<sbyte> TUShuffleMask1 = Vector128.Create(0, 1, 2, 3, 8, 4, 5, 8, 6, 7, -1, -1, -1, -1, -1, -1);
	}
}
