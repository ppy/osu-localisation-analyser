using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LocalisationAnalyser.Localisation;
using LocalisationAnalyser.Tools.Php;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace LocalisationAnalyser.Tools
{
    internal class Program
    {
        private const string reader_type = "System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string writer_type = "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

        public static async Task Main(string[] args)
        {
            var toResx = new Command("to-resx", "Generates resource (.resx) files from all localisations in the target project.")
            {
                new Argument("project-file")
                {
                    Description = "The C# project (.csproj) file."
                },
            };

            var phpToResx = new Command("from-php", "Generates side-by-side resource (.resx) files for all PHP files recursively in the target directory.")
            {
                new Argument("directory")
                {
                    Description = "The directory to find all PHP files in."
                }
            };

            toResx.Handler = CommandHandler.Create<string>(projectToResX);
            phpToResx.Handler = CommandHandler.Create<string>(phpToResX);

            await new RootCommand("osu! Localisation Tools")
            {
                toResx,
                phpToResx
            }.InvokeAsync(args);
        }

        private static async Task projectToResX(string projectFile)
        {
            Console.WriteLine($"Converting all localisation files in {projectFile}...");

            MSBuildLocator.RegisterDefaults();

            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(projectFile);

            var localisationFiles = project.Documents.Where(d => d.Folders.SequenceEqual(SyntaxTemplates.PROJECT_RELATIVE_LOCALISATION_PATH.Split('/')))
                                           .Where(d => d.Name.EndsWith(".cs"))
                                           .Where(d => Path.GetFileNameWithoutExtension(d.Name).EndsWith(SyntaxTemplates.STRINGS_FILE_SUFFIX))
                                           .ToArray();

            if (localisationFiles.Length == 0)
            {
                Console.WriteLine("No localisation files found in project.");
                return;
            }

            foreach (var file in localisationFiles)
            {
                Console.WriteLine($"Processing {file.Name}...");

                LocalisationFile localisationFile;
                using (var stream = File.OpenRead(file.FilePath))
                    localisationFile = await LocalisationFile.ReadAsync(stream);

                string resxFile = Path.Combine(Path.GetDirectoryName(file.FilePath)!, $"{localisationFile.Prefix}.resx");

                using (var fs = File.Open(resxFile, FileMode.Create, FileAccess.ReadWrite))
                using (var resWriter = new ResXResourceWriter(fs, getResourceTypeName))
                {
                    foreach (var member in localisationFile.Members)
                        resWriter.AddResource(member.Key, member.EnglishText);
                    resWriter.Generate();
                }

                Console.WriteLine($"  -> {resxFile}");
            }
        }

        private static async Task phpToResX(string directory)
        {
            var files = Directory.EnumerateFiles(directory, "*.php", SearchOption.AllDirectories).ToArray();

            if (files.Length == 0)
            {
                Console.WriteLine("No PHP files found in the target directory.");
                return;
            }

            foreach (var file in files)
            {
                Console.WriteLine($"Processing {file}...");

                string fileContents = await File.ReadAllTextAsync(file);

                // Get the first array.
                int firstBracket = fileContents.IndexOf('[');
                int lastBracket = fileContents.LastIndexOf(']') + 1;
                fileContents = fileContents.Substring(firstBracket, lastBracket - firstBracket);

                var arraySyntax = PhpArraySyntaxNode.Parse(new PhpTokeniser(fileContents));
                string resxFile = Path.ChangeExtension(file, ".resx");

                using (var fs = File.Open(resxFile, FileMode.Create, FileAccess.ReadWrite))
                using (var resWriter = new ResXResourceWriter(fs, getResourceTypeName))
                {
                    foreach (var (key, value) in getTokensAndValues(arraySyntax))
                        resWriter.AddResource(key, value);
                    resWriter.Generate();
                }

                Console.WriteLine($"  -> {resxFile}");
            }
        }

        private static IEnumerable<(string key, string value)> getTokensAndValues(PhpArraySyntaxNode arraySyntax, string? currentKey = null)
        {
            currentKey ??= string.Empty;

            foreach (var i in arraySyntax.Elements)
            {
                var key = i.Key.Text;

                string elementKey = $"{currentKey}{key}";

                switch (i.Value)
                {
                    case PhpArraySyntaxNode nestedArray:
                        foreach (var nestedPair in getTokensAndValues(nestedArray, $"{elementKey}."))
                            yield return nestedPair;

                        break;

                    case PhpStringLiteralSyntaxNode str:
                        string stringValue = str.Text;

                        var formatMatches = Regex.Matches(stringValue, @":[a-zA-Z\-_]+");
                        int formatIndex = formatMatches.Count - 1;

                        while (formatIndex >= 0)
                        {
                            var match = formatMatches[formatIndex];
                            stringValue = $"{stringValue[..match.Index]}{{{formatIndex}}}{stringValue[(match.Index + match.Length)..]}";
                            formatIndex--;
                        }

                        yield return (elementKey, stringValue);

                        break;
                }
            }
        }

        private static string getResourceTypeName(Type type)
        {
            if (type == typeof(ResXResourceReader))
                return reader_type;
            if (type == typeof(ResXResourceWriter))
                return writer_type;

            throw new ArgumentException("Unexpected resource type.", nameof(type));
        }
    }
}
