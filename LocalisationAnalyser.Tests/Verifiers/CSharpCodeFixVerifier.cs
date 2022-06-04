// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace LocalisationAnalyser.Tests.Verifiers
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public static async Task VerifyCodeFixAsync((string filename, string contents)[] sources, (string filename, string contents)[] fixedSources, bool brokenAnalyserConfigFiles = false)
            => await VerifyCodeFixAsync(sources, DiagnosticResult.EmptyDiagnosticResults, fixedSources, brokenAnalyserConfigFiles);

        public static async Task VerifyCodeFixAsync((string filename, string contents)[] sources, DiagnosticResult expected, (string filename, string contents)[] fixedSources,
                                                    bool brokenAnalyserConfigFiles = false)
            => await VerifyCodeFixAsync(sources, new[] { expected }, fixedSources, brokenAnalyserConfigFiles);

        public static async Task VerifyCodeFixAsync((string filename, string contents)[] sources, DiagnosticResult[] expected, (string filename, string contents)[] fixedSources,
                                                    bool brokenAnalyserConfigFiles = false)
        {
            var test = new Test();

            foreach (var s in sources)
            {
                switch (Path.GetExtension(s.filename))
                {
                    case ".cs":
                        test.TestState.Sources.Add((s.filename, SourceText.From(s.contents, Encoding.UTF8)));
                        break;

                    case ".editorconfig" when !brokenAnalyserConfigFiles:
                        test.TestState.AnalyzerConfigFiles.Add(s);
                        break;

                    default:
                        test.TestState.AdditionalFiles.Add(s);
                        break;
                }
            }

            foreach (var s in fixedSources)
            {
                switch (Path.GetExtension(s.filename))
                {
                    case ".cs":
                        test.FixedState.Sources.Add((s.filename, SourceText.From(s.contents, Encoding.UTF8)));
                        break;

                    case ".editorconfig" when !brokenAnalyserConfigFiles:
                        test.FixedState.AnalyzerConfigFiles.Add(s);
                        break;

                    default:
                        test.FixedState.AdditionalFiles.Add(s);
                        break;
                }
            }

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
