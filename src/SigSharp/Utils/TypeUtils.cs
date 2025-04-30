namespace SigSharp.Utils;

internal static class TypeUtils
{
    /// <summary>
    /// Fast check to determine if a type is nullable with special case for string
    /// </summary>
    /// <typeparam name="T">Any type to check</typeparam>
    /// <returns>True if T can be null</returns>
    public static bool IsNullableByDefault<T>(bool treatStringAsValueType = true)
    {
        if (default(T) is not null)
            return false;

        return !treatStringAsValueType || typeof(T) != typeof(string);
    }
}