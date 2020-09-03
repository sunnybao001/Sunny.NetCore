using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public abstract class LongInterface : System.Text.Json.Serialization.JsonConverter<long>
	{
		public static readonly LongInterface Singleton;// = new LongInterface();
		//private LongInterface() { }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.ValueSpan;
			if (str.Length != 16) throw new InvalidCastException();
			if (!TryParseLong(in Unsafe.As<byte, Vector128<short>>(ref Unsafe.AsRef(in str.GetPinnableReference())), out var v)) throw new InvalidCastException();
			return v;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override unsafe void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
		{
			var vector = LongToUtf8_16(value);
			writer.WriteStringValue(new ReadOnlySpan<byte>(&vector, 16));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool TryParse(string str, out long value)
		{
			var vector = AsciiInterface.Singleton.UnicodeToAscii_16(in Unsafe.As<char, Vector256<short>>(ref Unsafe.AsRef(in str.GetPinnableReference()))).AsInt16();
			return TryParseLong(in vector, out value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe string LongToString(long value)
		{
			var vector = Avx2.ConvertToVector256Int16(LongToUtf8_16(value));
			return new string((char*)&vector, 0, 16);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe Vector128<byte> LongToUtf8_16(long value)
		{
			var vector = Ssse3.Shuffle(*(Vector128<sbyte>*)&value, ShuffleMask).AsInt16();
			return Sse2.Add(Sse2.Or(Sse2.ShiftRightLogical(vector, 4), Sse2.ShiftLeftLogical(Sse2.And(vector, LowMask), 8)), ShortCharA).AsByte();
		}
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//private unsafe bool TryParseLong(in Vector128<short> input, out long value)
		//{
		//	value = default;
		//	var vector = Sse2.Subtract(input, ShortCharA);
		//	if (!Sse41.TestZ(vector, ShortN15)) return false;
		//	value = Sse41.X64.Extract(Ssse3.Shuffle(Sse2.Or(Sse2.ShiftLeftLogical(vector, 4), Sse2.ShiftRightLogical(vector, 8)).AsSByte(), NShuffleMask).AsInt64(), 0);
		//	return true;
		//}
		protected abstract bool TryParseLong(in Vector128<short> input, out long value);
		static LongInterface()
		{
			var type = AsciiInterface.ModuleBuilder.DefineType(nameof(LongInterface), System.Reflection.TypeAttributes.Sealed, typeof(LongInterface));
			var method = type.DefineMethod(nameof(TryParseLong),
				System.Reflection.MethodAttributes.Family | System.Reflection.MethodAttributes.Virtual | System.Reflection.MethodAttributes.HideBySig,
				System.Reflection.CallingConventions.Standard | System.Reflection.CallingConventions.HasThis,
				typeof(bool),
				Type.EmptyTypes,
				Type.EmptyTypes,
				new Type[] { typeof(Vector128<short>).MakeByRefType(), typeof(long).MakeByRefType() },
				new Type[][] { new Type[] { typeof(InAttribute) }, Type.EmptyTypes },
				new Type[][] { Type.EmptyTypes, Type.EmptyTypes });

			method.SetCustomAttribute(new CustomAttributeBuilder(typeof(MethodImplAttribute).GetConstructor(new Type[] { typeof(MethodImplOptions) }), new object[] { MethodImplOptions.AggressiveInlining }));
			var il = method.GetILGenerator();
			var loc0 = il.DeclareLocal(typeof(Vector128<short>));
			var loc1 = il.DeclareLocal(typeof(bool));
			il.Emit(OpCodes.Ldarg_2);//1
			il.Emit(OpCodes.Ldarg_1);//2
			il.Emit(OpCodes.Ldobj, typeof(Vector128<short>));//2
			il.Emit(OpCodes.Ldarg_0);//3
			il.Emit(OpCodes.Ldfld, typeof(LongInterface).GetField(nameof(ShortCharA), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));//3
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.Subtract), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);//2
			il.Emit(OpCodes.Dup);//3
			il.Emit(OpCodes.Ldarg_0);//4
			il.Emit(OpCodes.Ldfld, typeof(LongInterface).GetField(nameof(ShortN15), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));//4
			il.EmitCall(OpCodes.Call, typeof(Sse41).GetMethod(nameof(Sse41.TestZ), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);//3
			il.Emit(OpCodes.Stloc_1);//2
			il.Emit(OpCodes.Dup);//3
			il.Emit(OpCodes.Stloc_0);//2
			il.Emit(OpCodes.Ldc_I4_4);//3
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.ShiftLeftLogical), new Type[] { typeof(Vector128<short>), typeof(byte) }), null);//2
			il.Emit(OpCodes.Ldloc_0);//3
			il.Emit(OpCodes.Ldc_I4_8);//4
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical), new Type[] { typeof(Vector128<short>), typeof(byte) }), null);//3
			il.EmitCall(OpCodes.Call, typeof(Sse2).GetMethod(nameof(Sse2.Or), new Type[] { typeof(Vector128<short>), typeof(Vector128<short>) }), null);//2
			//il.EmitCall(OpCodes.Call, typeof(Vector128).GetMethod(nameof(Vector128.AsSByte)).MakeGenericMethod(typeof(short)), null);//2
			il.Emit(OpCodes.Ldarg_0);//3
			il.Emit(OpCodes.Ldfld, typeof(LongInterface).GetField(nameof(NShuffleMask), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));//3
			il.EmitCall(OpCodes.Call, typeof(Ssse3).GetMethod(nameof(Ssse3.Shuffle), new Type[] { typeof(Vector128<sbyte>), typeof(Vector128<sbyte>) }), null);//2
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
			il.Emit(OpCodes.Stind_I8);//0
			//il.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(Type.EmptyTypes));
			//il.Emit(OpCodes.Throw);
			il.Emit(OpCodes.Ldloc_1);
			il.Emit(OpCodes.Ret);
			
			Singleton = (LongInterface)Activator.CreateInstance(type.CreateType());
		}
		internal protected readonly Vector128<short> ShortCharA = Avx2.ExtractVector128(GuidInterface.Singleton.ShortCharA, 0);
		internal protected Vector128<short> ShortN15 = Avx2.ExtractVector128(GuidInterface.Singleton.ShortN15, 0);
		internal Vector128<short> LowMask = Avx2.ExtractVector128(GuidInterface.Singleton.LowMask, 0);
		internal Vector128<short> HeightMask = Vector128.Create((short)0xF0);
		private readonly Vector128<sbyte> ShuffleMask = Vector128.Create(7, -1, 6, -1, 5, -1, 4, -1, 3, -1, 2, -1, 1, -1, 0, -1);
		internal protected readonly Vector128<sbyte> NShuffleMask = Vector128.Create(14, 12, 10, 8, 6, 4, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0);
	}
}
