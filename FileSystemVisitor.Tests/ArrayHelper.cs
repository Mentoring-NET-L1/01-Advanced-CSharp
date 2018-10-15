using System;

namespace FileSystemVisitor.Tests
{
    internal static class ArrayHelper
    {
        public static bool Compare<T>(T[] arr1, T[] arr2, Comparison<T> comparison)
        {
            if (arr1 == arr2)
                return true;

            if (arr1 == null || arr2 == null)
                return false;

            if (arr1.Length != arr2.Length)
                return false;

            Array.Sort(arr1);
            Array.Sort(arr2);

            for (var i = 0; i < arr1.Length; i++)
            {
                if (comparison(arr1[i], arr2[i]) != 0)
                    return false;
            }

            return true;
        }
    }
}
