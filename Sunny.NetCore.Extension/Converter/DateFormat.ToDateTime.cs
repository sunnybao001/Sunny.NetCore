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
		private unsafe bool Utf8_10ToDate(in Vector128<sbyte> input, out DateTime value)
		{
			var vector = Ssse3.Shuffle(input, TDShuffleMask1);
			value = default;
			if (!Utf8Bit2ToNumber(*(long*)&vector, out var v)) return false;
			var vf = (short*)&v;
			value = new DateTime(vf[0] * 100 + vf[1], vf[2], vf[3]);
			return true;
		}
		private unsafe bool Utf8_19ToDate(in Vector256<sbyte> input, out DateTime value)
		{
			var vector = Avx2.Shuffle(input, TDShuffleMask);
			((int*)&vector)[3] = ((int*)&vector)[4];	//多拷贝2个字节是为了让后面的TestZ不报错
			value = default;
			if (!Utf8Bit2ToNumber(ref *(Vector128<sbyte>*)&vector, out var v)) return false;
			var vf = (short*)&v;
			value = new DateTime(vf[0] * 100 + vf[1], vf[2], vf[3], vf[4], vf[5], vf[6]);
			return true;
		}
		private unsafe bool Utf8Bit2ToNumber(long input, out long value)
		{
			value = default;
			var vector = Sse2.Subtract(Sse41.ConvertToVector128Int16((sbyte*)&input), ShortChar01);
			if (!Sse41.TestZ(vector, ShortN151)) return false;
			vector = Sse2.MultiplyLow(vector, Int101);  //双数位置的乘数为0，所以不用担心short溢出
			vector = Ssse3.HorizontalAdd(vector, default);
			value = *(long*)&vector;
			return true;
		}
		private unsafe bool Utf8Bit2ToNumber(ref Vector128<sbyte> input, out Vector128<short> value)
		{
			value = default;
			var vector = Avx2.Subtract(Avx2.ConvertToVector256Int16(input), ShortChar0);
			if (!Avx.TestZ(vector, ShortN15)) return false;
			vector = Avx2.MultiplyLow(vector, Int10);  //双数位置的乘数为0，所以不用担心short溢出
			value = Ssse3.HorizontalAdd(*(Vector128<short>*)&vector, ((Vector128<short>*)&vector)[1]);
			return true;
		}
		private Vector256<short> Int10 = Vector256.Create(10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1);
		private Vector128<short> Int101;
		private Vector256<sbyte> TDShuffleMask = Vector256.Create(0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 14, 15, -1, -1, -1, -1, 1, 2, 1, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
		private Vector128<sbyte> TDShuffleMask1 = Vector128.Create(0, 1, 2, 3, 5, 6, 8, 9, -1, -1, -1, -1, -1, -1, -1, -1);
		private Vector256<short> ShortN15 = Vector256.Create((sbyte)~15).AsInt16();
		private Vector128<short> ShortN151;
	}
}
