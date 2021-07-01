// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = LocalisationAnalyser.Tests.Verifiers.CSharpCodeFixVerifier<
    LocalisationAnalyser.Analysers.StringCanBeLocalisedAnalyser,
    LocalisationAnalyser.Tests.CodeFixes.Providers.LocaliseClassStringCodeFixMockProvider>;

namespace LocalisationAnalyser.Tests.CodeFixes
{
    public class LocaliseClassStringCodeFixTests : AbstractLocaliseStringCodeFixTests
    {
        [Theory]
        [InlineData("BasicString")]
        [InlineData("VerbatimString")]
        [InlineData("InterpolatedString")]
        [InlineData("InterpolatedStringWithQuotes")]
        [InlineData("CustomPrefix")]
        public async Task Check(string name) => await RunTest(name);

        protected override Task Verify((string filename, string content)[] sources, (string filename, string content)[] fixedSources)
            => VerifyCS.VerifyCodeFixAsync(sources, fixedSources);
    }
}
