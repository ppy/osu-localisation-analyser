// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LocalisationAnalyser.Tests.Helpers.IO;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Tests.Analysers
{
    public abstract class AbstractAnalyserTests<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        private const string resources_namespace = "LocalisationAnalyser.Tests.Resources";

        protected async Task Check(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            var sourceFiles = new List<string>
            {
                $"{resources_namespace}.LocalisableString.txt",
                $"{resources_namespace}.TranslatableString.txt",
            };

            foreach (var f in resourceNames.Where(n => n.StartsWith($"{resources_namespace}.Analysers.{name}")))
                sourceFiles.Add(f);

            await Verifiers.CSharpAnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(
                sourceFiles.Select(f => (getFileNameFromResourceName($"{resources_namespace}.Analysers.{name}", f), readResourceStream(assembly, f))).ToArray());
        }

        private string getFileNameFromResourceName(string resourceNamespace, string resourceName)
        {
            string extension = Path.GetExtension(resourceName);

            resourceName = resourceName.Replace(resourceNamespace, string.Empty)[1..]
                                       .Replace(extension, string.Empty)
                                       .Replace('.', '/');

            // .txt files are converted to .cs.
            resourceName = extension == ".txt" ? $"{resourceName}.cs" : $"{resourceName}{extension}";

            return new MockFileSystem().Path.GetFullPath(resourceName);
        }

        private string readResourceStream(Assembly asm, string resource)
        {
            using (var stream = asm.GetManifestResourceStream(resource)!)
            using (var sr = new StreamReader(stream))
                return sr.ReadToEnd();
        }
    }
}
