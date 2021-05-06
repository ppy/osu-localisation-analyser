// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace LocalisationAnalyser.Tests.Verifiers
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public static async Task VerifyCodeFixAsync(string[] sources, string[] fixedSources)
            => await VerifyCodeFixAsync(sources, DiagnosticResult.EmptyDiagnosticResults, fixedSources);

        public static async Task VerifyCodeFixAsync(string[] sources, DiagnosticResult expected, string[] fixedSources)
            => await VerifyCodeFixAsync(sources, new[] { expected }, fixedSources);

        public static async Task VerifyCodeFixAsync(string[] sources, DiagnosticResult[] expected, string[] fixedSources)
        {
            var test = new Test();

            foreach (var s in sources)
                test.TestState.Sources.Add(s);

            foreach (var s in fixedSources)
                test.FixedState.Sources.Add(s);

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
