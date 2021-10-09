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
			var v = Ssse3.HorizontalAdd(Sse2.MultiplyLow(vector, this.Int101), Vector128<short>.Zero).AsUInt16();
			var v2 = Avx.Or(Sse2.Or(Sse2.CompareLessThan(Max, v.AsInt16()), Sse2.CompareLessThan(v.AsInt16(), Min)).AsInt16(), Sse2.And(vector, this.ShortN151));	  //此处最大宽度128，使用or比使用insert更快
			if (!Sse41.TestZ(v2, v2))
			{
				value = default;
				return false;
			}
			var year = Sse2.Extract(v, 0) + Sse2.Extract(v, 1);
			var month = Sse2.Extract(v, 2);
			var day = Sse2.Extract(v, 3);
			value = new DateTime(year, month, day);    //寄存器优化
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe bool Utf8_19ToDate(in Vector256<sbyte> input, out DateTime value)
		{
			var vector = Avx2.Subtract(Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(Avx2.PermuteVar8x32(Avx2.Shuffle(input, TDShuffleMask).AsInt32(), Permute), 0).AsSByte()), ShortChar0);
			var v0 = Avx2.MultiplyLow(vector, Int10).AsInt16();
			//不能使用256位指令，因为256位指令的顺序与128位不同
			var v = Ssse3.HorizontalAdd(v0.GetLower(), Avx2.ExtractVector128(v0, 1)).AsUInt16();    //双数位置的乘数为0，所以不用担心short溢出
			var v2 = Avx2.Or(Avx.InsertVector128(Sse2.CompareLessThan(Max, v.AsInt16()).ToVector256Unsafe(), Sse2.CompareLessThan(v.AsInt16(), Min), 1), Avx2.And(vector, ShortN15));   //有一个原始数组的长度是256，使用insert比使用or减少1个extract指令
			if (!Avx.TestZ(v2, v2))
			{
				value = default;
				return false;
			}
			var year = Sse2.Extract(v, 0) + Sse2.Extract(v, 1);
			var month = Sse2.Extract(v, 2);
			var day = Sse2.Extract(v, 3);
			var hour = Sse2.Extract(v, 4);
			var minu = Sse2.Extract(v, 5);
			var seco = Sse2.Extract(v, 6);
			value = new DateTime(year, month, day, hour, minu, seco);    //寄存器优化
			return true;
		}
		private Vector256<short> Int10 = Vector256.Create(1000, 100, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 0, 0);
		internal readonly Vector128<short> Int101;
		private Vector256<sbyte> TDShuffleMask = Vector256.Create(0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 14, 15, -1, -1, -1, -1, 1, 2, 1, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
		private Vector256<int> Permute = Vector256.Create(0, 1, 2, 4, 3, 3, 3, 3);
		private Vector128<sbyte> TDShuffleMask1 = Vector128.Create(0, 1, 2, 3, 5, 6, 8, 9, -1, -1, -1, -1, -1, -1, -1, -1);
		private Vector256<short> ShortN15 = Vector256.Create((sbyte)~15).AsInt16();
		private Vector128<short> Max = Vector128.Create(9900, 99, 12, 31, 24, 60, 60, 0);
		private Vector128<short> Min = Vector128.Create(0, 0, 1, 1, 0, 0, 0, 0);
		internal readonly Vector128<short> ShortN151;
	}
}
