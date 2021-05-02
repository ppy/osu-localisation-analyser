using System;

namespace LocalisationAnalyser.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            string empty = "";
            string emptyInterpolated = $"";

            string simpleLiteral = "Hello world!";
            string concatenatedLiteral = "hello" + "world!";

            string interpolated = $"simple: {empty} complex: {(empty == "a" ? "b" : "c")} method: {string.Format(empty, "a")}";

            Console.WriteLine("Hello World!");
        }
    }
}
