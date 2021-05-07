// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LocalisationAnalyser.Tests.CodeFixes
{
    public abstract class AbstractLocaliseStringCodeFixTests
    {
        private const string resources_namespace = "LocalisationAnalyser.Tests.Resources";

        public async Task RunTest(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            var requiredFiles = new List<(string filename, string content)>
            {
                ("LocalisableString.cs", readResourceStream(assembly, $"{resources_namespace}.LocalisableString.txt")),
                ("TranslatableString.cs", readResourceStream(assembly, $"{resources_namespace}.TranslatableString.txt"))
            };

            var sourceFiles = new List<(string filename, string content)>(requiredFiles);
            var fixedFiles = new List<(string filename, string content)>(requiredFiles);

            string sourcesNamespace = $"{resources_namespace}.CodeFixes.{name}.Sources";
            string fixedNamespace = $"{resources_namespace}.CodeFixes.{name}.Fixed";

            foreach (var file in resourceNames.Where(n => n.StartsWith(sourcesNamespace)))
                sourceFiles.Add((getFileNameFromResourceName(sourcesNamespace, file), readResourceStream(assembly, file)));
            foreach (var file in resourceNames.Where(n => n.StartsWith(fixedNamespace)))
                fixedFiles.Add((getFileNameFromResourceName(fixedNamespace, file), readResourceStream(assembly, file)));

            // Files added to the solution via the codefix are always appended to the end. This is always going to be the localisation file.
            // We need ot maintain a consistent order for roslyn to assert correctly.
            sourceFiles = sourceFiles.OrderBy(f => f.filename == "Program.cs" ? -1 : 1).ToList();
            fixedFiles = fixedFiles.OrderBy(f => f.filename == "Program.cs" ? -1 : 1).ToList();

            await Verify(sourceFiles.ToArray(), fixedFiles.ToArray());
        }

        private string getFileNameFromResourceName(string resourceNamespace, string resourceName)
        {
            resourceName = resourceName.Replace(resourceNamespace, string.Empty)[1..]
                                       .Replace(".txt", string.Empty)
                                       .Replace('.', '/');

            return $"{Path.GetFileName(resourceName)}.cs";
        }

        protected abstract Task Verify((string filename, string content)[] sources, (string filename, string content)[] fixedSources);

        private string readResourceStream(Assembly asm, string resource)
        {
            using (var stream = asm.GetManifestResourceStream(resource)!)
            using (var sr = new StreamReader(stream))
                return sr.ReadToEnd();
        }
    }
}
