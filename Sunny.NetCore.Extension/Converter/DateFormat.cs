using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public abstract partial class DateFormat : System.Text.Json.Serialization.JsonConverter<DateTime>
	{
		public static readonly DateFormat Singleton;// = new DateFormat();
		public DateFormat() {
			ShortChar01 = Avx2.ExtractVector128(ShortChar0, 0);
			ShortD1 = Avx2.ExtractVector128(ShortD, 0);
			ShortBit2X10Vector1 = Avx2.ExtractVector128(ShortBit2X10Vector, 0);
			Int101 = Avx2.ExtractVector128(Int10, 0);
			Short101 = Avx2.ExtractVector128(Short10, 0);
			SbyteMax1 = Avx2.ExtractVector128(SbyteMax, 0);
			ShortN151 = Avx2.ExtractVector128(ShortN15, 0);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var s = reader.ValueSpan;
			if (TryParseDateTime(s, out var value)) return value;
			throw new ArgumentException("DateTime的格式不正确");
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			if (value.TimeOfDay.Ticks == default)
			{
				var r = DateToUtf8_10(value);
				writer.WriteStringValue(new ReadOnlySpan<byte>(&r, 10));
			}
			else
			{
				var r = DateTimeToUtf8_19(value);
				writer.WriteStringValue(new ReadOnlySpan<byte>(&r, 19));
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe string DateTimeToString(DateTime value)
		{
			if (value.TimeOfDay.Ticks == default)
			{
				var vector = Avx2.ConvertToVector256Int16(DateToUtf8_10(value));
				return new string((char*)&vector, 0, 10);
			}
			else
			{
				var r = DateTimeToUtf8_19(value);
				var f = stackalloc Vector256<short>[2];
				AsciiInterface.AsciiToUnicode(r, f);
				return new string((char*)f, 0, 19);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool TryParseDateTime(string str, out DateTime value)
		{
			var vector = AsciiInterface.Singleton.UnicodeToAscii_32(ref Unsafe.As<char, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())));
			return TryParseDateTime(new ReadOnlySpan<byte>(&vector, str.Length), out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseDateTime(ReadOnlySpan<byte> input, out DateTime value)
		{
			bool success = true;
			if (input.Length == 19 | input.Length == 20) return Utf8_19ToDate(in Unsafe.As<byte, Vector256<sbyte>>(ref Unsafe.AsRef(in input.GetPinnableReference())), out value);
			if (input.Length == 10) return Utf8_10ToDate(in Unsafe.As<byte, Vector128<sbyte>>(ref Unsafe.AsRef(in input.GetPinnableReference())), out value);
			if (input.Length < 10 & input.Length > 7)
			{
				int start = 0, length = 4;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int year, out _);
				start += length + 1;
				length = 0;
				while (length != 2 & input[start + length] != '-') ++length;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int month, out _);
				start += length + 1;
				length = 0;
				while ((start + length != input.Length) && (length != 2 & input[start + length] != '-')) ++length;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int day, out _);
				if (!success)
				{
					value = default;
					return false;
				}
				value = new DateTime(year, month, day);
				return true;
			}
			value = default;
			return false;
		}
		static DateFormat()
		{
			var type = AsciiInterface.ModuleBuilder.DefineType(nameof(DateFormat), System.Reflection.TypeAttributes.Sealed, typeof(DateFormat));
			var method = type.DefineMethod(nameof(Utf8Bit2ToNumber),
				System.Reflection.MethodAttributes.Family | System.Reflection.MethodAttributes.Virtual | System.Reflection.MethodAttributes.HideBySig,
				System.Reflection.CallingConventions.Standard | System.Reflection.CallingConventions.HasThis,
				typeof(bool),
				Type.EmptyTypes,
				Type.EmptyTypes,
				new Type[] { typeof(long), typeof(long).MakeByRefType() },
				new Type[][] { Type.EmptyTypes, Type.EmptyTypes },
				new Type[][] { Type.EmptyTypes, Type.EmptyTypes });

			method.SetCustomAttribute(new CustomAttributeBuilder(typeof(MethodImplAttribute).GetConstructor(new Type[] { typeof(MethodImplOptions) }), new object[] { MethodImplOptions.AggressiveInlining }));
			var il = method.GetILGenerator();
			var loc0 = il.DeclareLocal(typeof(bool));
			il.Emit(OpCodes.Ldarg_2);//1
			il.Emit(OpCodes.Ldarga_S, 1);//2
			il.EmitCall(OpCodes.Call, typeof(Sse41).GetMethod(nameof(Sse41.ConvertToVector128Int16), new Type[] { typeof(sbyte*) }), null);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(ShortChar01), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.Subtract), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Dup);//3
			il.Emit(OpCodes.Ldarg_0);//4
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(ShortN151), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));//4
			il.EmitCall(OpCodes.Call, typeof(Sse41).GetMethod(nameof(Sse41.TestZ), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);//3
			il.Emit(OpCodes.Stloc_0);//2
			il.Emit(OpCodes.Ldarg_0);//3
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(Int101), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.MultiplyLow), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Dup);
			il.EmitCall(OpCodes.Call, typeof(Ssse3).GetMethod(nameof(Ssse3.HorizontalAdd), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			if (Sse41.X64.IsSupported)
			{
				//il.EmitCall(OpCodes.Call, typeof(Vector128).GetMethod(nameof(Vector128.AsInt64)).MakeGenericMethod(typeof(sbyte)), null);//2
				il.Emit(OpCodes.Ldc_I4_0);//3
				il.EmitCall(OpCodes.Call, typeof(Sse41.X64).GetMethod(nameof(Sse41.X64.Extract), new Type[] { typeof(Vector128<long>), typeof(byte) }), null);//2
			}
			else
			{
				var loc1 = il.DeclareLocal(typeof(Vector128<sbyte>));
				il.Emit(OpCodes.Stloc_1);//1
				il.Emit(OpCodes.Ldloca_S, 1);//2
				il.Emit(OpCodes.Ldind_I8);//2
			}
			il.Emit(OpCodes.Stind_I8);//0
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ret);

			method = type.DefineMethod(nameof(NumberToUtf8Bit2),
			System.Reflection.MethodAttributes.Family | System.Reflection.MethodAttributes.Virtual | System.Reflection.MethodAttributes.HideBySig,
			System.Reflection.CallingConventions.Standard | System.Reflection.CallingConventions.HasThis,
			typeof(long),
			Type.EmptyTypes,
			Type.EmptyTypes,
			new Type[] { typeof(Vector128<int>).MakeByRefType() },
			new Type[][] { new Type[] { typeof(System.Runtime.InteropServices.InAttribute) } },
			new Type[][] { Type.EmptyTypes });

			method.SetCustomAttribute(new CustomAttributeBuilder(typeof(MethodImplAttribute).GetConstructor(new Type[] { typeof(MethodImplOptions) }), new object[] { MethodImplOptions.AggressiveInlining }));
			il = method.GetILGenerator();
			il.Emit(OpCodes.Ldarg_1);//1
			il.Emit(OpCodes.Ldobj, typeof(Vector128<int>));//1
			il.Emit(OpCodes.Dup);//2
			il.Emit(OpCodes.Ldc_I4, 16);//3
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.ShiftLeftLogical), new Type[] { typeof(Vector128<int>), typeof(byte) }), null);//2
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.Or), new Type[] { typeof(Vector128<int>), typeof(Vector128<int>) }), null);//1
			//il.EmitCall(OpCodes.Call, typeof(Vector128).GetMethod(nameof(Vector128.AsInt16)).MakeGenericMethod(typeof(int)), null);//1
			il.Emit(OpCodes.Ldarg_0);//2
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(SbyteMax1), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.And), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(ShortBit2X10Vector1), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.MultiplyLow), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Ldc_I4_7);
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical), new Type[] { typeof(Vector128<short>), typeof(byte) }), null);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(SbyteMax1), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.And), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(ShortD1), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.MultiplyLow), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Ldc_I4_7);
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical), new Type[] { typeof(Vector128<short>), typeof(byte) }), null);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(SbyteMax1), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.And), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(Short101), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.MultiplyLow), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.Subtract), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(DateFormat).GetField(nameof(ShortChar01), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			il.Emit(OpCodes.Dup);
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.PackUnsignedSaturate), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);
			if (Sse41.X64.IsSupported)
			{
				//il.EmitCall(OpCodes.Call, typeof(Vector128).GetMethod(nameof(Vector128.AsInt64)).MakeGenericMethod(typeof(sbyte)), null);//2
				il.Emit(OpCodes.Ldc_I4_0);//3
				il.EmitCall(OpCodes.Call, typeof(Sse41.X64).GetMethod(nameof(Sse41.X64.Extract), new Type[] { typeof(Vector128<long>), typeof(byte) }), null);//2
			}
			else
			{
				var loc2 = il.DeclareLocal(typeof(Vector128<sbyte>));
				il.Emit(OpCodes.Stloc_2);//1
				il.Emit(OpCodes.Ldloca_S, 2);//2
				il.Emit(OpCodes.Ldind_I8);//2
			}
			il.Emit(OpCodes.Ret);

			Singleton = (DateFormat)Activator.CreateInstance(type.CreateType());
		}
		private Vector256<short> ShortChar0 = Vector256.Create((short)'0');
		private Vector256<short> ShortD = Vector256.Create((short)0xD);   //D CD CCCD
																				 //7 11 19 分别最大值255 2047 524287
																				 //最大化精度需要容器：short int long
		private Vector256<short> ShortBit2X10Vector = Vector256.Create(0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80);
		protected readonly Vector128<short> ShortChar01;
		protected readonly Vector128<short> ShortD1;
		protected readonly Vector128<short> ShortBit2X10Vector1;
	}
}
