using System;
using System.Collections.Generic;

namespace JetBlack.Promises
{
    internal static class Extensions
    {
        public static void Nothing()
        {
        }

        public static void Nothing<T>(T arg)
        {
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
                action(item);
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            var index = 0;
            foreach (var item in enumerable)
                action.Invoke(item, index++);
        }

        public static void TryCatch(this Action action, Action<Exception> onError)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                onError(error);
            }
        }

        public static void TryCatch<T>(this Action<T> action, T arg, Action<Exception> onError)
        {
            try
            {
                action(arg);
            }
            catch (Exception error)
            {
                onError(error);
            }
        }

        public static void TryCatch<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2, Action<Exception> onError)
        {
            try
            {
                action(arg1, arg2);
            }
            catch (Exception error)
            {
                onError(error);
            }
        }
    }
}
