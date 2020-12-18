
using Sunny.NetCore.Extension.Converter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Generator
{
	public static class LongValueGenerator
	{
		/// <summary>
		/// 将生成器注入到ef core框架中
		/// </summary>
		/// <typeparam name="T">PropertyBuilder'long'</typeparam>
		/// <param name="propertyBuilder">ef属性构造器</param>
		public static T RegisterEfCoreValueGenerator<T>(T propertyBuilder) where T : class
		{
			if (EfCoreValueGeneratorMethod == null)
			{
				lock (lc)
				{
					if (EfCoreValueGeneratorMethod == null)
					{
						INITEfCoreValueGenerator<T>();
					}
				}
			}
			return (T)EfCoreValueGeneratorMethod.Invoke(propertyBuilder, null);
		}
		private static void INITEfCoreValueGenerator<T>()
		{
			var efAssembly = typeof(T).Assembly;
			var type = AsciiInterface.ModuleBuilder.DefineType(nameof(LongValueGenerator), TypeAttributes.Sealed, efAssembly.GetType("Microsoft.EntityFrameworkCore.ValueGeneration.ValueGenerator"));
			var method = type.DefineMethod("NextValue",
				MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
				CallingConventions.Standard | CallingConventions.HasThis,
				typeof(object),
				new Type[] { efAssembly.GetType("Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry") });
			method.SetCustomAttribute(new CustomAttributeBuilder(typeof(MethodImplAttribute).GetConstructor(new Type[] { typeof(MethodImplOptions) }), new object[] { MethodImplOptions.AggressiveOptimization }));
			var il = method.GetILGenerator();
			il.EmitCall(OpCodes.Call, typeof(LongValueGenerator).GetMethod(nameof(NextValue)), null);
			il.Emit(OpCodes.Box, typeof(long));
			il.Emit(OpCodes.Ret);

			var property = type.DefineProperty("GeneratesTemporaryValues", PropertyAttributes.None, typeof(bool), null);

			method = type.DefineMethod("get_GeneratesTemporaryValues",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
				CallingConventions.Standard | CallingConventions.HasThis,
				typeof(bool),
				Type.EmptyTypes);
			il = method.GetILGenerator();
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ret);

			property.SetGetMethod(method);
			var gType = type.CreateType();
			EfCoreValueGeneratorMethod = typeof(T).GetMethod("HasValueGenerator", Type.EmptyTypes).MakeGenericMethod(gType);
		}
		//public override bool GeneratesTemporaryValues => false;

		//protected override object NextValue(EntityEntry entry) => NextValue();
		public static long NextValue() => GetId(DateTime.UtcNow.Ticks);
		public static long OldToNewId(long old)
		{
			var dt = OldIdToDateTime(old);
			return GetId(dt.Ticks);
		}
		public static long GetMinValue(DateTime dt) => GetPrefix(dt.Ticks);
		public static long GetMaxValue(DateTime dt) => GetPrefix(dt.Ticks) + 0x1000000;
		private unsafe static DateTime OldIdToDateTime(long old)
		{
			int* f = (int*)&old;
			var ticks = (long)f[1];
			ticks <<= 24;
			ticks += new DateTime(2010, 1, 1).Ticks;
			return new DateTime(ticks, DateTimeKind.Utc);
		}
		private unsafe static long GetId(long ticks)
		{
			int r;
			do
			{
				r = Guid.NewGuid().GetHashCode();
			} while (r == default);
			//DateTime的Ticks前两位永远是0，但是第一位是符号位不可用
			long id = ticks << 1;
			byte* f = (byte*)&id;   //随机数占用24位，剩余40位，前1位舍去，密度为DateTime的41位，大约半秒多改变一次值
			byte* rf = (byte*)&r;
			if (X86Base.IsSupported)
			{
				f[0] = rf[0];
				f[1] = rf[1];
				f[2] = rf[2];
			}
			else
			{
				f[5] = rf[1];
				f[6] = rf[2];
				f[7] = rf[3];
			}
			return id;
		}
		private unsafe static long GetPrefix(long ticks)
		{
			long id = ticks << 1;
			byte* f = (byte*)&id;
			if (X86Base.IsSupported) f[0] = f[1] = f[2] = 0;
			else f[5] = f[6] = f[7] = 0;
			return id;
		}
		private static object lc = new object();
		private static MethodInfo EfCoreValueGeneratorMethod;
	}
}
