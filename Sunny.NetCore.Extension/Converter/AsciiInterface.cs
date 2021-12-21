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
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public unsafe Vector128<byte> UnicodeToAscii_16(in Vector256<short> input)
		{
			var vector = Avx2.And(input, AsciiMax);
			return Sse2.PackUnsignedSaturate(Avx2.ExtractVector128(vector, 0), Avx2.ExtractVector128(vector, 1));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public unsafe Vector256<byte> UnicodeToAscii_32(ref Vector256<short> input)
		{
			return Avx2.Permute4x64(Avx2.PackUnsignedSaturate(Avx2.And(input, AsciiMax), Avx2.And(Unsafe.Add(ref input, 1), AsciiMax)).AsInt64(), 0b1101_1000).AsByte();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static unsafe void AsciiToUnicode(Vector256<byte> input, ref Vector256<short> output)
		{
			output = Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(input, 0));
			Unsafe.Add(ref output, 1) = Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(input, 1));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static unsafe void AsciiToUnicode(Vector128<byte> input, ref Vector256<short> output)
		{
			output = Avx2.ConvertToVector256Int16(input);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ref TR StringTo<T, TR>(ReadOnlySpan<T> str) => ref Unsafe.As<T, TR>(ref Unsafe.AsRef(in str.GetPinnableReference()));
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ref TR StringTo<T, TR>(Span<T> str) => ref Unsafe.As<T, TR>(ref str.GetPinnableReference());
		private Vector256<short> AsciiMax = Vector256.Create((short)sbyte.MaxValue);
		public static readonly System.Reflection.Emit.ModuleBuilder ModuleBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(new System.Reflection.AssemblyName("Sunny.NetCore.Extrnsion.Emit"), System.Reflection.Emit.AssemblyBuilderAccess.Run).DefineDynamicModule("Converter");
		internal Func<int, string> FastAllocateString = (Func<int, string>)typeof(string).GetMethod("FastAllocateString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).CreateDelegate(typeof(Func<int, string>));
		//[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Native)]
		//internal static extern string FastAllocateString(int length);
	}
}
