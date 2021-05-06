// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Verify = LocalisationAnalyser.Tests.Verifiers.CSharpAnalyzerVerifier<LocalisationAnalyser.Analysers.LocalisationAnalyser>;

namespace LocalisationAnalyser.Tests.Analysers
{
    public class LocalisationAnalyserTests
    {
        private static int i = 5;
        private static string x = $"{i}";

        [Theory]
        [InlineData("BasicString")]
        [InlineData("EmptyString")]
        [InlineData("InterpolatedString")]
        [InlineData("NumericString")]
        [InlineData("StringConcatenation")]
        [InlineData("VerbatimString")]
        public async Task Check(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            var requiredFiles = new[]
            {
                resourceNames.Single(n => n.Contains("LocalisableString.txt")),
                resourceNames.Single(n => n.Contains("TranslatableString.txt"))
            };

            var sourceFiles = requiredFiles.Append(resourceNames.Single(n => n.Contains($"ANA_{name}")));

            await Verify.VerifyAnalyzerAsync(sourceFiles.Select(f => readResourceStream(assembly, f)).ToArray());
        }

        private string readResourceStream(Assembly asm, string resource)
        {
            using (var stream = asm.GetManifestResourceStream(resource)!)
            using (var sr = new StreamReader(stream))
                return sr.ReadToEnd();
        }
    }
}
