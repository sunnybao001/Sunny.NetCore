using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public static class MvcInterface
	{
		/// <summary>
		/// 将long转换器和guid转换器注入到mvc接口
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="modelBinderProviders"></param>
		public static void RegisterMvcModelBinderProviders<T>(IList<T> modelBinderProviders) where T : class
		{
			var interfaceMethod = typeof(T).GetMethod("GetBinder");
			var contextParam = interfaceMethod.GetParameters()[0].ParameterType;
			var contextReturn = interfaceMethod.ReturnType;

			var type = AsciiInterface.ModuleBuilder.DefineType(nameof(MvcInterface), TypeAttributes.Sealed, null, new Type[] { typeof(T) });
			var longguidF = type.DefineField("longguid", typeof(Guid), FieldAttributes.Private);
			var guidguidF = type.DefineField("guidguid", typeof(Guid), FieldAttributes.Private);
			var longModelBiner = type.DefineField("LongModelBiner", contextReturn, FieldAttributes.Private);
			var guidModelBiner = type.DefineField("GuidModelBiner", contextReturn, FieldAttributes.Private);

			var method = type.DefineMethod("GetBinder",
				MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
				CallingConventions.Standard | CallingConventions.HasThis,
				contextReturn,
				new Type[] { contextParam });
			var il = method.GetILGenerator();
			var loc0 = il.DeclareLocal(typeof(Guid));
			il.Emit(OpCodes.Ldarg_1);
			var metadataGetMethod = contextParam.GetProperty("Metadata").GetGetMethod();
			il.EmitCall(OpCodes.Callvirt, metadataGetMethod, null);
			il.EmitCall(OpCodes.Call, metadataGetMethod.ReturnType.GetProperty("ModelType").GetGetMethod(), null);
			il.EmitCall(OpCodes.Callvirt, typeof(Type).GetProperty(nameof(Type.GUID)).GetGetMethod(), null);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc_0);
			EqualityTypeGuid(il, longguidF, longModelBiner);
			il.Emit(OpCodes.Ldloc_0);
			EqualityTypeGuid(il, guidguidF, guidModelBiner);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ret);
			type.DefineMethodOverride(method, interfaceMethod);

			var type0 = type.CreateType();
			var providers = (T)Activator.CreateInstance(type0);
			type0.GetField("longguid", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(providers, typeof(long).GUID);
			type0.GetField("guidguid", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(providers, typeof(Guid).GUID);
			type0.GetField("LongModelBiner", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(providers, GenerateModelBiner<LongInterface, long>(contextReturn));
			type0.GetField("GuidModelBiner", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(providers, GenerateModelBiner<GuidInterface, Guid>(contextReturn));

			modelBinderProviders.Insert(0, providers);
		}
		private static void EqualityTypeGuid(ILGenerator il, FieldBuilder guidField, FieldBuilder binerField)
		{
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, guidField);
			il.EmitCall(OpCodes.Call, typeof(Guid).GetMethod("op_Equality", new Type[] { typeof(Guid), typeof(Guid) }), null);
			var label = il.DefineLabel();
			il.Emit(OpCodes.Brfalse_S, label);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, binerField);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(label);
		}
		private static object GenerateModelBiner<T, TR>(Type modelBinderInterface)
		{
			var type = AsciiInterface.ModuleBuilder.DefineType(typeof(T).Name + "_Biner", TypeAttributes.Sealed, null, new Type[] { modelBinderInterface });
			var converterF = type.DefineField("Converter", typeof(T), FieldAttributes.Private);
			var completedTaskF = type.DefineField("CompletedTask", typeof(Task), FieldAttributes.Private);

			var bindingInterfaceMethod = modelBinderInterface.GetMethod("BindModelAsync");
			var bindingParam = bindingInterfaceMethod.GetParameters()[0].ParameterType;
			var method = type.DefineMethod("BindModelAsync",
				MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
				CallingConventions.Standard | CallingConventions.HasThis,
				typeof(Task),
				new Type[] { bindingParam });
			var il = method.GetILGenerator();
			var loc0 = il.DeclareLocal(typeof(string));
			var loc1 = il.DeclareLocal(typeof(TR));
			il.Emit(OpCodes.Ldarg_1);//1
			var valueProviderMethod = bindingParam.GetProperty("ValueProvider").GetGetMethod();
			il.EmitCall(OpCodes.Callvirt, valueProviderMethod, null);//1
			il.Emit(OpCodes.Ldarg_1);//2
			il.EmitCall(OpCodes.Callvirt, bindingParam.GetProperty("ModelName").GetGetMethod(), null);//2
			il.Emit(OpCodes.Dup);//3
			il.Emit(OpCodes.Stloc_0);//2
			var getValueMethod = valueProviderMethod.ReturnType.GetMethod("GetValue");
			il.EmitCall(OpCodes.Callvirt, getValueMethod, null);//1
			il.Emit(OpCodes.Dup);//2
			il.Emit(OpCodes.Ldsfld, getValueMethod.ReturnType.GetField("None", BindingFlags.Public | BindingFlags.Static));//3
			il.EmitCall(OpCodes.Call, getValueMethod.ReturnType.GetMethod("op_Equality"), null);//2
			var label = il.DefineLabel();
			il.Emit(OpCodes.Brfalse_S, label);//1
			il.Emit(OpCodes.Pop);//0
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, completedTaskF);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(label);
			var loc2 = il.DeclareLocal(getValueMethod.ReturnType);
			il.Emit(OpCodes.Stloc_2);//0
			il.Emit(OpCodes.Ldarg_1);//1
			var getModelStateMethod = bindingParam.GetProperty("ModelState").GetGetMethod();
			il.EmitCall(OpCodes.Callvirt, getModelStateMethod, null);//1
			il.Emit(OpCodes.Ldloc_0);//2
			il.Emit(OpCodes.Ldloc_2);//3
			il.EmitCall(OpCodes.Call, getModelStateMethod.ReturnType.GetMethod("SetModelValue", new Type[] { typeof(string), getValueMethod.ReturnType }), null);//0
			il.Emit(OpCodes.Ldarg_0);//1
			il.Emit(OpCodes.Ldfld, converterF);//1
			il.Emit(OpCodes.Ldloca_S, 2);//2
			il.EmitCall(OpCodes.Call, getValueMethod.ReturnType.GetProperty("FirstValue").GetGetMethod(), null);//2
			il.Emit(OpCodes.Dup);//3
			il.EmitCall(OpCodes.Call, typeof(string).GetMethod(nameof(string.IsNullOrEmpty)), null);//3
			label = il.DefineLabel();
			il.Emit(OpCodes.Brfalse_S, label);//2
			il.Emit(OpCodes.Pop);//1
			il.Emit(OpCodes.Pop);//0
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, completedTaskF);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldloca_S, 1);//3
			il.EmitCall(OpCodes.Call, typeof(T).GetMethod(nameof(LongInterface.TryParse)), null);//1
			label = il.DefineLabel();
			il.Emit(OpCodes.Brtrue_S, label);//0
			il.Emit(OpCodes.Ldarg_1);//1
			il.EmitCall(OpCodes.Callvirt, getModelStateMethod, null);//1
			il.Emit(OpCodes.Ldloc_0);//2
			il.Emit(OpCodes.Ldstr, $"Author value must be an {typeof(TR).Name}.");//3
			il.EmitCall(OpCodes.Call, getModelStateMethod.ReturnType.GetMethod("TryAddModelError", new Type[] { typeof(string), typeof(string) }), null);//1
			il.Emit(OpCodes.Pop);//0
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, completedTaskF);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldarg_1);//1
			il.Emit(OpCodes.Ldloc_1);//2
			il.Emit(OpCodes.Box, typeof(TR));//2
			var setResultMethod = bindingParam.GetProperty("Result").GetSetMethod();
			il.EmitCall(OpCodes.Call, setResultMethod.GetParameters()[0].ParameterType.GetMethod("Success", BindingFlags.Public | BindingFlags.Static), null);//2
			il.EmitCall(OpCodes.Callvirt, setResultMethod, null);//0
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, completedTaskF);
			il.Emit(OpCodes.Ret);
			type.DefineMethodOverride(method, bindingInterfaceMethod);

			var type0 = type.CreateType();
			var modelBiner = Activator.CreateInstance(type0);
			type0.GetField("Converter", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(modelBiner, typeof(T).GetField("Singleton", BindingFlags.Public | BindingFlags.Static).GetValue(null));
			type0.GetField("CompletedTask", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(modelBiner, Task.CompletedTask);
			return modelBiner;
		}
	}
}
