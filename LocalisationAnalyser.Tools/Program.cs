using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Humanizer;
using LocalisationAnalyser.Localisation;
using LocalisationAnalyser.Tools.Php;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
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
                new Argument("project-file") { Description = "The C# project (.csproj) file." }
            };

            var phpToResx = new Command("from-php", "Converts localisations from the target osu!web directory into the target project.")
            {
                new Argument("osu-web directory") { Description = "The osu!web installation directory." },
                new Argument("project-file") { Description = "The target C# project (.csproj) file to place the localisations in." }
            };

            toResx.Handler = CommandHandler.Create<string>(projectToResX);
            phpToResx.Handler = CommandHandler.Create<string, string>(convertPhp);

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

        private static async Task convertPhp(string osuWeb, string projectFile)
        {
            string projectLocalisationDirectory = Path.Combine(Path.GetDirectoryName(projectFile)!, "Localisation", "Web");
            Directory.CreateDirectory(projectLocalisationDirectory);

            string webLocalisationDirectory = Path.Combine(osuWeb, "resources", "lang");

            if (!Directory.Exists(webLocalisationDirectory))
            {
                Console.WriteLine("No 'lang' directory in the osu!web installation.");
                return;
            }

            string enLangDirectory = Path.Combine(webLocalisationDirectory, "en");

            if (!Directory.Exists(enLangDirectory))
            {
                Console.WriteLine("'en' localisation not found in the osu!web installation.");
                return;
            }

            Console.WriteLine("Processing english localisations...");

            foreach (var file in Directory.EnumerateFiles(enLangDirectory, "*.php", SearchOption.TopDirectoryOnly))
            {
                Console.WriteLine($"Processing {file}...");

                string name = Path.GetFileNameWithoutExtension(file).Pascalize();

                string targetLocalisation = Path.Combine(projectLocalisationDirectory, Path.ChangeExtension($"{name}Strings", "cs"));
                string targetResources = Path.Combine(projectLocalisationDirectory, Path.ChangeExtension(name, "resx"));

                // Get the first array from the PHP file.
                string phpContents = await File.ReadAllTextAsync(file);
                int firstBracket = phpContents.IndexOf('[');
                int lastBracket = phpContents.LastIndexOf(']') + 1;
                phpContents = phpContents.Substring(firstBracket, lastBracket - firstBracket);

                var localisations = getLocalisationsFromPhpArray(PhpArraySyntaxNode.Parse(new PhpTokeniser(phpContents))).ToArray();

                // Create the .resx file.
                using (var fs = File.Open(targetResources, FileMode.Create, FileAccess.ReadWrite))
                using (var resWriter = new ResXResourceWriter(fs, getResourceTypeName))
                {
                    foreach (var member in localisations)
                        resWriter.AddResource(member.Key, member.EnglishText);
                    resWriter.Generate();
                }

                // Create the .cs file.
                var localisationFile = new LocalisationFile("osu.Game.Localisation.Web", Path.GetFileNameWithoutExtension(targetLocalisation), name, localisations);
                using (var fs = File.Open(targetLocalisation, FileMode.Create, FileAccess.ReadWrite))
                    await localisationFile.WriteAsync(fs, new AdhocWorkspace());

                Console.WriteLine($"  -> {targetResources}");
                Console.WriteLine($"  -> {targetLocalisation}");
            }
        }

        private static IEnumerable<LocalisationMember> getLocalisationsFromPhpArray(PhpArraySyntaxNode arraySyntax, string? currentKey = null)
        {
            currentKey ??= string.Empty;

            foreach (var i in arraySyntax.Elements)
            {
                string thisKey = i.Key.Text;
                string fullKey = $"{currentKey}{thisKey}";

                switch (i.Value)
                {
                    case PhpArraySyntaxNode nestedArray:
                        foreach (var nested in getLocalisationsFromPhpArray(nestedArray, $"{fullKey}."))
                            yield return nested;

                        break;

                    case PhpStringLiteralSyntaxNode str:
                        string stringValue = str.Text;

                        // Find all "format parameters" in the localisation string of type :text .
                        var formatStrings = Regex.Matches(stringValue, @":([a-zA-Z\-_]+)");

                        // Each format parameter needs to be assigned a C# format index and parameter name (for the C# class).
                        var formatIndices = new List<int>();
                        var formatParamNames = new List<string>();

                        for (int j = 0; j < formatStrings.Count; j++)
                        {
                            var match = formatStrings[j];
                            var paramName = match.Groups[1].Captures[0].Value;

                            int existingIndex = formatParamNames.IndexOf(paramName);

                            if (existingIndex >= 0)
                            {
                                // If this is a duplicated format string within the same string, refer to the same index as the others and forego addition of a new parameter.
                                formatIndices.Add(formatIndices[existingIndex]);
                                continue;
                            }

                            formatIndices.Add(formatIndices.Count == 0 ? 0 : formatIndices.Max() + 1);
                            formatParamNames.Add(paramName);
                        }

                        // Replace the format parameters in the original string with the respective C# counterpart ({0}, {1}, ...).
                        for (int j = formatStrings.Count - 1; j >= 0; j--)
                        {
                            var match = formatStrings[j];
                            stringValue = $"{stringValue[..match.Index]}{{{formatIndices[j]}}}{stringValue[(match.Index + match.Length)..]}";
                        }

                        yield return new LocalisationMember(
                            generateMemberNameFromKey(fullKey),
                            fullKey,
                            stringValue,
                            formatParamNames.Select(p => new LocalisationParameter("string", p.Camelize())).ToArray());

                        break;
                }
            }
        }

        private static string generateMemberNameFromKey(string key)
        {
            var memberName = new StringBuilder();

            foreach (var ns in key.Split('.'))
            {
                string formatted = ns.Trim('_');

                // If the string is just a simple _, add some default string.
                if (string.IsNullOrEmpty(formatted))
                    formatted = "Default";

                // Only humanise if there's at least two unique characters in the stirng.
                // This handles cases like "mm" and "MM" (time/date formatting strings).
                if (ns.Distinct().Count() > 1)
                    formatted = ns.Humanize().Dehumanize();

                memberName.Append(formatted);
            }

            return memberName.ToString();
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
