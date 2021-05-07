// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
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
        private const string resources_namespace = "LocalisationAnalyser.Tests.Resources";

        [Theory]
        [InlineData("BasicString")]
        public async Task Check(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            var requiredFiles = new List<(string filename, string content)>
            {
                ("LocalisableString.cs", readResourceStream(assembly, $"{resources_namespace}.LocalisableString.txt")),
                ("TranslatableString.cs", readResourceStream(assembly, $"{resources_namespace}.TranslatableString.txt"))
            };

            string sourcesNamespace = $"{resources_namespace}.CodeFixes.{name}.Sources";
            var sourceFiles = new List<(string filename, string content)>(requiredFiles);

            foreach (var file in resourceNames.Where(n => n.StartsWith(sourcesNamespace)))
            {
                string filename = file.Replace(sourcesNamespace, string.Empty).Replace(".txt", ".cs")[1..];
                sourceFiles.Add((filename, readResourceStream(assembly, file)));
            }

            string fixedNamespace = $"{resources_namespace}.CodeFixes.{name}.Fixed";
            var fixedFiles = new List<(string filename, string content)>(requiredFiles);

            foreach (var file in resourceNames.Where(n => n.StartsWith(fixedNamespace)))
            {
                string filename = file.Replace(fixedNamespace, string.Empty).Replace(".txt", ".cs")[1..];
                fixedFiles.Add((filename, readResourceStream(assembly, file)));
            }

            await Verify.VerifyCodeFixAsync(sourceFiles.ToArray(), fixedFiles.ToArray());
        }

        private string readResourceStream(Assembly asm, string resource)
        {
            using (var stream = asm.GetManifestResourceStream(resource)!)
            using (var sr = new StreamReader(stream))
                return sr.ReadToEnd();
        }
    }
}
