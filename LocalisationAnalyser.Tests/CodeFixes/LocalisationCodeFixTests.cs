// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Verify = LocalisationAnalyser.Tests.Verifiers.CSharpCodeFixVerifier<LocalisationAnalyser.Analysers.LocalisationAnalyser, LocalisationAnalyser.Tests.CodeFixes.MockLocalisationCodeFixProvider>;

namespace LocalisationAnalyser.Tests.CodeFixes
{
    public class LocalisationCodeFixTests
    {
        [Theory]
        [InlineData("BasicString")]
        public async Task LocaliseLiteralString(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            var requiredFiles = new[]
            {
                resourceNames.Single(n => n.Contains("LocalisableString.txt")),
                resourceNames.Single(n => n.Contains("TranslatableString.txt"))
            };

            var sourceFiles = requiredFiles.Concat(resourceNames.Where(n => n.Contains($"CF_{name}_Source")));
            var fixedFiles = requiredFiles.Concat(resourceNames.Where(n => n.Contains($"CF_{name}_Fixed")));

            await Verify.VerifyCodeFixAsync(
                sourceFiles.Select(f => readResourceStream(assembly, f)).ToArray(),
                fixedFiles.Select(f => readResourceStream(assembly, f)).ToArray());
        }

        private string readResourceStream(Assembly asm, string resource)
        {
            using (var stream = asm.GetManifestResourceStream(resource)!)
            using (var sr = new StreamReader(stream))
                return sr.ReadToEnd();
        }
    }
}
