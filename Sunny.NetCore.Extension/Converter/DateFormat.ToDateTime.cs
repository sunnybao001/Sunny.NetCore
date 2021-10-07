﻿using System;
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
		private unsafe bool Utf8_10ToDate(in Vector128<sbyte> input, out DateTime value)
		{
			var vector = Sse2.Subtract(Sse41.ConvertToVector128Int16(Ssse3.Shuffle(input, TDShuffleMask1)), this.ShortChar01);
			if (!Sse41.TestZ(vector, this.ShortN151))
			{
				value = default;
				return false;
			}
			vector = Sse2.MultiplyLow(vector, this.Int101);
			var v = Ssse3.HorizontalAdd(vector, vector).AsByte();
			var v1 = Sse2.Or(Sse2.CompareLessThan(Max, v.AsInt16()), Sse2.CompareLessThan(v.AsInt16(), Min)).AsInt32();
			if (Sse41.X64.IsSupported ? 0 != Sse41.X64.Extract(v1.AsInt64(), 0) : 0 != (Sse41.Extract(v1, 0) | Sse41.Extract(v1, 1)))
			{
				value = default;
				return false;
			}
			var year = Sse41.Extract(v, 0) * 100 + Sse41.Extract(v, 2);
			var month = Sse41.Extract(v, 4);
			var day = Sse41.Extract(v, 6);
			value = new DateTime(year, month, day);    //寄存器优化
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe bool Utf8_19ToDate(in Vector256<sbyte> input, out DateTime value)
		{
			var vector = Avx2.Subtract(Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(Avx2.PermuteVar8x32(Avx2.Shuffle(input, TDShuffleMask).AsInt32(), Permute), 0).AsSByte()), ShortChar0);
			if (!Avx.TestZ(vector, ShortN15))
			{
				value = default;
				return false;
			}
			var v0 = Avx2.MultiplyLow(vector, Int10).AsInt64();
			var v = Ssse3.HorizontalAdd(Avx2.ExtractVector128(v0, 0).AsInt16(), Avx2.ExtractVector128(v0, 1).AsInt16()).AsByte();    //双数位置的乘数为0，所以不用担心short溢出
			var v1 = Sse2.Or(Sse2.CompareLessThan(Max, v.AsInt16()), Sse2.CompareLessThan(v.AsInt16(), Min));
			if (!Sse41.TestZ(v1, v1))
			{
				value = default;
				return false;
			}
			var year = Sse41.Extract(v, 0) * 100 + Sse41.Extract(v, 2);
			var month = Sse41.Extract(v, 4);
			var day = Sse41.Extract(v, 6);
			var hour = Sse41.Extract(v, 8);
			var minu = Sse41.Extract(v, 10);
			var seco = Sse41.Extract(v, 12);
			value = new DateTime(year, month, day, hour, minu, seco);    //寄存器优化
			return true;
		}
		private Vector256<short> Int10 = Vector256.Create(10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 0, 0);
		internal readonly Vector128<short> Int101;
		private Vector256<sbyte> TDShuffleMask = Vector256.Create(0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 14, 15, -1, -1, -1, -1, 1, 2, 1, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
		private Vector256<int> Permute = Vector256.Create(0, 1, 2, 4, 3, 3, 3, 3);
		private Vector128<sbyte> TDShuffleMask1 = Vector128.Create(0, 1, 2, 3, 5, 6, 8, 9, -1, -1, -1, -1, -1, -1, -1, -1);
		private Vector256<short> ShortN15 = Vector256.Create((sbyte)~15).AsInt16();
		private Vector128<short> Max = Vector128.Create(99, 99, 12, 31, 24, 60, 60, 0);
		private Vector128<short> Min = Vector128.Create(0, 1, 1, 1, 0, 0, 0, 0);
		internal readonly Vector128<short> ShortN151;
	}
}
