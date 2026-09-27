using System;
using System.Collections.Generic;

namespace ShrinkCart.BuildTools
{
    /// <summary>收集并打印检查结果。控制台输出只用英文（Windows PowerShell 默认按 OEM 代码页解码子进程输出）。</summary>
    internal sealed class Report
    {
        private readonly List<string> _failures = new List<string>();
        private int _warnings;

        internal int FailureCount => _failures.Count;

        internal void Pass(string message)
        {
            Write("[PASS] ", message, ConsoleColor.Green);
        }

        internal void Info(string message)
        {
            Write("[INFO] ", message, ConsoleColor.Gray);
        }

        internal void Warn(string message)
        {
            _warnings++;
            Write("[WARN] ", message, ConsoleColor.Yellow);
        }

        internal void Fail(string message)
        {
            _failures.Add(message);
            Write("[FAIL] ", message, ConsoleColor.Red);
        }

        internal void Section(string title)
        {
            Console.WriteLine();
            Console.WriteLine("== " + title);
        }

        /// <returns>进程退出码：有失败项时为 1。</returns>
        internal int Finish()
        {
            Console.WriteLine();
            Console.WriteLine("Failures: " + _failures.Count + "; warnings: " + _warnings);
            return _failures.Count == 0 ? 0 : 1;
        }

        private static void Write(string prefix, string message, ConsoleColor color)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(prefix);
            Console.ForegroundColor = previous;
            Console.WriteLine(message);
        }
    }

    /// <summary>无法继续执行的前置条件错误（如找不到仓库根目录或编译产物）。</summary>
    internal sealed class BuildToolException : Exception
    {
        internal BuildToolException(string message)
            : base(message)
        {
        }
    }
}
