using System.Reflection;
using System.Reflection.Emit;

namespace ReusableModules
{
	/// <summary>
	/// Generator class for creating custom copy operations, using meta-programming.
	/// Useful to avoid having to write the same common copy instructions across data types,
	/// like copying the items between two array fields.
	/// NOTE: currently this is set up specially to only use structs, but obviously can be easily extended to classes too.
	/// </summary>
	/// <typeparam name="T">The type of struct to generate a copy method for</typeparam>
	public static class DynamicCopy<T> where T : struct
	{
		public static readonly RefCopyDelegate<T> IlEmitCopy;

		static DynamicCopy()
		{
			Type paramType = typeof(T);
			Type paramAsRefType = paramType.MakeByRefType();
			FieldInfo[] allFields = paramType.GetFields(Constants.BINDING_FLAGS_FOR_ALL_FIELDS);

			MethodAttributes methodAttr = MethodAttributes.Public | MethodAttributes.Static;

			DynamicMethod dm = new("CopyFrom", methodAttr, CallingConventions.Standard, null, new Type[] { paramAsRefType, paramAsRefType }, paramType.Module, true);
			ILGenerator il = dm.GetILGenerator();

			// Array index. We don't declare this until we know we need it
			LocalBuilder? i = null;

			foreach (FieldInfo fi in allFields)
			{
				if (fi.FieldType.IsPrimitive || fi.FieldType.IsEnum)
				{
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldfld, fi);
					il.Emit(OpCodes.Stfld, fi);
				}
				else if (fi.FieldType.IsArray)
				{
					Type elementType = fi.FieldType.GetElementType()!;

					// TODO: implement support for arrays of non-primitive element types
					if (!elementType.IsPrimitive && !elementType.IsEnum) continue;

					Label exitArrayAssignment = il.DefineLabel();
					Label loopStart = il.DefineLabel();

					il.Emit(OpCodes.Nop);

					// if (lhs.Array == null) return;
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldfld, fi);
					il.Emit(OpCodes.Brfalse_S, exitArrayAssignment);

					// if (rhs.Array == null) return;
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldfld, fi);
					il.Emit(OpCodes.Brfalse_S, exitArrayAssignment);

					// if (lhs.Array.Length != rhs.Array.Length) return;
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldfld, fi);
					il.Emit(OpCodes.Ldlen);
					il.Emit(OpCodes.Conv_I4);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldfld, fi);
					il.Emit(OpCodes.Ldlen);
					il.Emit(OpCodes.Conv_I4);
					il.Emit(OpCodes.Ceq);
					il.Emit(OpCodes.Brfalse_S, exitArrayAssignment);
					//il.Emit(OpCodes.Bne_Un_S, exitArrayAssignment);

					il.Emit(OpCodes.Nop);

					// int i = 0;
					i ??= il.DeclareLocal(typeof(int));
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Stloc, i);

					// Beginning of loop
					il.MarkLabel(loopStart);

					// if (i >= lhs.Array.Length) break;
					il.Emit(OpCodes.Ldloc, i);
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldfld, fi);
					il.Emit(OpCodes.Ldlen);
					il.Emit(OpCodes.Conv_I4);
					il.Emit(OpCodes.Clt);
					il.Emit(OpCodes.Brfalse_S, exitArrayAssignment);

					il.Emit(OpCodes.Nop);

					// lhs.Array[i] = rhs.Array[i];
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldfld, fi);
					il.Emit(OpCodes.Ldloc, i);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldfld, fi);
					il.Emit(OpCodes.Ldloc, i);
					il.Emit(OpCodes.Ldelem, elementType);
					il.Emit(OpCodes.Stelem, elementType);

					il.Emit(OpCodes.Nop);

					// i++;
					il.Emit(OpCodes.Ldloc, i);
					il.Emit(OpCodes.Ldc_I4_1);
					il.Emit(OpCodes.Add);
					il.Emit(OpCodes.Stloc, i);

					// Loop to where we evaluate whether i is greater than the array length
					il.Emit(OpCodes.Br_S, loopStart);

					il.Emit(OpCodes.Nop);

					// Loop exit
					il.MarkLabel(exitArrayAssignment);
				}
				// TODO: implement support for other types
			}

			il.Emit(OpCodes.Ret);

			IlEmitCopy = dm.CreateDelegate<RefCopyDelegate<T>>();
		}
	}
}
