using System.Reflection;

namespace ReusableModules
{
	// Delegates
	public delegate void RefCopyDelegate<T>(ref T source, ref T target) where T : struct;

	public static class Constants
	{
		public const BindingFlags BINDING_FLAGS_FOR_ALL_FIELDS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	}
}
