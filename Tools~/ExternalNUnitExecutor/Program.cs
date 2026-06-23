using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

internal static class Program
{
    private static int Main()
    {
        return MainAsync().GetAwaiter().GetResult();
    }

    private static async Task<int> MainAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var testMethods = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .SelectMany(type => type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => HasAttribute<TestAttribute>(method))
                .Select(method => new TestCase(type, method)))
            .OrderBy(testCase => testCase.Type.FullName)
            .ThenBy(testCase => testCase.Method.Name)
            .ToList();

        var passed = 0;
        var failed = 0;

        foreach (var testCase in testMethods)
        {
            var testName = testCase.Type.FullName + "." + testCase.Method.Name;

            try
            {
                await RunTest(testCase);
                passed++;
                Console.WriteLine("PASS " + testName);
            }
            catch (Exception exception)
            {
                failed++;
                Console.WriteLine("FAIL " + testName);
                Console.WriteLine(FormatException(Unwrap(exception)));
            }
        }

        Console.WriteLine("SUMMARY total=" + testMethods.Count + " passed=" + passed + " failed=" + failed);
        return failed == 0 ? 0 : 1;
    }

    private static async Task RunTest(TestCase testCase)
    {
        var instance = Activator.CreateInstance(testCase.Type);
        var setUpMethods = GetLifecycleMethods<SetUpAttribute>(testCase.Type);
        var tearDownMethods = GetLifecycleMethods<TearDownAttribute>(testCase.Type);

        try
        {
            foreach (var setUpMethod in setUpMethods)
                await InvokeAndAwait(setUpMethod, instance);

            await InvokeAndAwait(testCase.Method, instance);
        }
        finally
        {
            foreach (var tearDownMethod in tearDownMethods)
                await InvokeAndAwait(tearDownMethod, instance);
        }
    }

    private static async Task InvokeAndAwait(MethodInfo method, object instance)
    {
        var result = method.Invoke(instance, Array.Empty<object>());

        if (result is Task task)
        {
            await task;
            return;
        }

        if (result is ValueTask valueTask)
            await valueTask;
    }

    private static MethodInfo[] GetLifecycleMethods<TAttribute>(Type type) where TAttribute : Attribute
    {
        return type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(HasAttribute<TAttribute>)
            .OrderBy(method => method.MetadataToken)
            .ToArray();
    }

    private static bool HasAttribute<TAttribute>(MethodInfo method) where TAttribute : Attribute
    {
        return method.GetCustomAttributes(typeof(TAttribute), true).Length > 0;
    }

    private static Exception Unwrap(Exception exception)
    {
        while (exception is TargetInvocationException targetException && targetException.InnerException != null)
            exception = targetException.InnerException;

        return exception;
    }

    private static string FormatException(Exception exception)
    {
        return exception.GetType().FullName + ": " + exception.Message + Environment.NewLine + exception.StackTrace;
    }

    private readonly struct TestCase
    {
        public readonly Type Type;
        public readonly MethodInfo Method;

        public TestCase(Type type, MethodInfo method)
        {
            Type = type;
            Method = method;
        }
    }
}
