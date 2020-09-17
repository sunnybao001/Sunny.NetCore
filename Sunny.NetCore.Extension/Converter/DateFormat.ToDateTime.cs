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
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private unsafe bool Utf8_10ToDate(in Vector128<sbyte> input, out DateTime value)
		{
			var vector = Ssse3.Shuffle(input, TDShuffleMask1);
			value = default;
			if (!Utf8Bit2ToNumber(*(long*)&vector, out var v)) return false;
			var vf = (short*)&v;
			value = new DateTime(vf[0] * 100 + vf[1], vf[2], vf[3]);
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private unsafe bool Utf8_19ToDate(in Vector256<sbyte> input, out DateTime value)
		{
			var vector = Avx2.PermuteVar8x32(Avx2.Shuffle(input, TDShuffleMask).AsInt32(), Permute);
			value = default;
			if (!Utf8Bit2ToNumber(ref *(Vector128<sbyte>*)&vector, out var v)) return false;
			var vf = (short*)&v;
			value = new DateTime(vf[0] * 100 + vf[1], vf[2], vf[3], vf[4], vf[5], vf[6]);
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private bool Utf8Bit2ToNumber(long input, out long value)
		{
			return Sse41.X64.IsSupported ? Utf8Bit2ToNumberX64(input, out value) : Utf8Bit2ToNumberX86(input, out value);   //会在JIT时进行静态判断
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private unsafe bool Utf8Bit2ToNumberX64(long input, out long value)
		{
			var vector = Sse2.Subtract(Sse41.ConvertToVector128Int16((sbyte*)&input), this.ShortChar01);
			var r = Sse41.TestZ(vector, this.ShortN151);
			vector = Sse2.MultiplyLow(vector, this.Int101);
			value = Sse41.X64.Extract(Ssse3.HorizontalAdd(vector, vector).AsInt64(), 0);
			return r;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private unsafe bool Utf8Bit2ToNumberX86(long input, out long value)
		{
			var vector = Sse2.Subtract(Sse41.ConvertToVector128Int16((sbyte*)&input), this.ShortChar01);
			var r = Sse41.TestZ(vector, this.ShortN151);
			vector = Sse2.MultiplyLow(vector, this.Int101);
			var v = Ssse3.HorizontalAdd(vector, vector).AsInt32();
#pragma warning disable CS0675 // 对进行了带符号扩展的操作数使用了按位或运算符
			value = Sse41.Extract(v, 0) | ((long)Sse41.Extract(v, 1) << 32);
#pragma warning restore CS0675 // 对进行了带符号扩展的操作数使用了按位或运算符
			return r;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private unsafe bool Utf8Bit2ToNumber(ref Vector128<sbyte> input, out Vector128<short> value)
		{
			var vector = Avx2.Subtract(Avx2.ConvertToVector256Int16(input), ShortChar0);
			var r = Avx.TestZ(vector, ShortN15);
			var v = Avx2.MultiplyLow(vector, Int10).AsInt64();
			value = Ssse3.HorizontalAdd(Avx2.ExtractVector128(v, 0).AsInt16(), Avx2.ExtractVector128(v, 1).AsInt16());    //双数位置的乘数为0，所以不用担心short溢出
			return r;
		}
		private Vector256<short> Int10 = Vector256.Create(10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1);
		internal readonly Vector128<short> Int101;
		private Vector256<sbyte> TDShuffleMask = Vector256.Create(0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 14, 15, -1, -1, -1, -1, 1, 2, 1, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
		private Vector256<int> Permute = Vector256.Create(0, 1, 2, 4, 3, 3, 3, 3);
		private Vector128<sbyte> TDShuffleMask1 = Vector128.Create(0, 1, 2, 3, 5, 6, 8, 9, -1, -1, -1, -1, -1, -1, -1, -1);
		private Vector256<short> ShortN15 = Vector256.Create((sbyte)~15).AsInt16();
		internal readonly Vector128<short> ShortN151;
	}
}
