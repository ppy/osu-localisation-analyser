// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = LocalisationAnalyser.Tests.Verifiers.CSharpCodeFixVerifier<
    LocalisationAnalyser.Analysers.StringCanBeLocalisedAnalyser,
    LocalisationAnalyser.Tests.CodeFixes.Providers.LocaliseClassStringCodeFixMockProvider>;

namespace LocalisationAnalyser.Tests.CodeFixes
{
    public class LocaliseClassStringCodeFixProviderTests : AbstractCodeFixProviderTests
    {
        [Theory]
        [InlineData("BasicString")]
        [InlineData("VerbatimString")]
        [InlineData("InterpolatedString")]
        [InlineData("InterpolatedStringWithQuotes")]
        [InlineData("CustomPrefixNamespace")]
        [InlineData("CustomResourceNamespace")]
        [InlineData("CustomLocalisationNamespace")]
        [InlineData("NestedClass")]
        [InlineData("LongString")]
        [InlineData("LicenseHeader")]
        [InlineData("DescriptionAttribute")]
        [InlineData("StringWithApostrophe")]
        [InlineData("SequentialCapitals")]
        [InlineData("SettingSourceAttribute")]
        public async Task Check(string name) => await RunTest(name);

        [Theory]
        [InlineData("CustomPrefixNamespace")]
        [InlineData("CustomFileNamespace")]
        public async Task CheckWithBrokenAnalyzerConfigFiles(string name) => await RunTest(name, true);

        protected override Task Verify((string filename, string content)[] sources, (string filename, string content)[] fixedSources, bool brokenAnalyserConfigFiles = false)
            => VerifyCS.VerifyCodeFixAsync(sources, fixedSources, brokenAnalyserConfigFiles);
    }
}
