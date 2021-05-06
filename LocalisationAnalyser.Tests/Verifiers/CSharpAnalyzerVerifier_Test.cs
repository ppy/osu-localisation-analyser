// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace LocalisationAnalyser.Tests.Verifiers
{
    public static partial class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        // Code fix tests support both analyzer and code fix testing. This test class is derived from the code fix test
        // to avoid the need to maintain duplicate copies of the customization work.
        public class Test : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
        {
        }
    }
}
