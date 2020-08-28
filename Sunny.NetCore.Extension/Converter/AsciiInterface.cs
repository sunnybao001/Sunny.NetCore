using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Sunny.NetCore.Extension.Converter
{
	class AsciiInterface
	{
		public static readonly AsciiInterface Singleton = new AsciiInterface();
		public AsciiInterface() { }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Vector128<byte> UnicodeToAscii_16(in Vector256<short> input)
		{
			var vector = Avx2.And(input, AsciiMax);
			var vectorf = (Vector128<short>*)&vector;
			return Sse2.PackUnsignedSaturate(vectorf[0], vectorf[1]);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Vector256<byte> UnicodeToAscii_32(ref Vector256<short> input)
		{
			return Avx2.Permute4x64(Avx2.PackUnsignedSaturate(Avx2.And(input, AsciiMax), Avx2.And(Unsafe.Add(ref input, 1), AsciiMax)).AsInt64(), 0b1101_1000).AsByte();
		}
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public unsafe Vector256<short> AsciiToUnicode(in Vector128<byte> input)
		//{
		//	return Avx2.ConvertToVector256Int16(input);
		//}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void AsciiToUnicode(Vector256<byte> input, Vector256<short>* output)
		{
			var vectorf = (Vector128<byte>*)&input;
			output[0] = Avx2.ConvertToVector256Int16(vectorf[0]);
			output[1] = Avx2.ConvertToVector256Int16(vectorf[1]);
		}
		private Vector256<short> AsciiMax = Vector256.Create((short)sbyte.MaxValue);
	}
}
