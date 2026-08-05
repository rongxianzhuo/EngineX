using System;
using System.Collections.Generic;
using System.Reflection;

namespace EngineX.Physics.Tests
{
    public static class TestRunner
    {
        private static readonly List<string> Failures = new List<string>();
        private static int _passed;

        public static void RunAll()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.GetCustomAttribute<TestAttribute>() == null) continue;
                    string name = $"{type.Name}.{method.Name}";
                    try
                    {
                        method.Invoke(null, null);
                        _passed++;
                        Console.WriteLine($"PASS {name}");
                    }
                    catch (Exception ex)
                    {
                        var inner = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                        Failures.Add($"{name}: {inner.GetType().Name}: {inner.Message}");
                        Console.WriteLine($"FAIL {name}: {inner.Message}");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"=== {_passed} passed, {Failures.Count} failed ===");
            if (Failures.Count > 0)
            {
                Environment.Exit(1);
            }
        }

        public static void Assert(bool condition, string message = "assertion failed")
        {
            if (!condition)
            {
                throw new AssertionException(message);
            }
        }

        public static void AssertEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException($"{message ?? "values differ"}: expected <{expected}>, actual <{actual}>");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
    }

    public sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message)
        {
        }
    }
}
