﻿using System;
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
using YamlDotNet.Serialization;

namespace LocalisationAnalyser.Tools
{
    internal class Program
    {
        private const string reader_type = "System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string writer_type = "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string web_namespace = "osu.Game.Localisation.Web";
        private const string en_lang_name = "en";
        private const string crowdin_mapping_file = "crowdin.yml";

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
            string webLocalisationDirectory = Path.Combine(osuWeb, "resources", "lang");

            if (!Directory.Exists(webLocalisationDirectory))
            {
                Console.WriteLine("No 'lang' directory in the osu!web installation.");
                return;
            }

            // Process the crowdin mapping file in order to reverse localisation mappings (e.g. pt-br -> pt-BR).
            // .NET uses the standard localisation scheme such that the original names conflict with other libraries and cause compiler errors due to case insensitivity.
            var settings = new Deserializer().Deserialize<dynamic>(await File.ReadAllTextAsync(Path.Combine(osuWeb, crowdin_mapping_file)));
            var mappings = ((IDictionary<object, object>)settings["files"][0]["languages_mapping"]["locale"])
                .ToDictionary(kvp => (string)kvp.Key, kvp => (string)kvp.Value);

            var reverseMappings = new Dictionary<string, string>();
            foreach (var (key, value) in mappings)
                reverseMappings.TryAdd(value, key);

            foreach (var file in Directory.EnumerateFiles(webLocalisationDirectory, "*.php", SearchOption.AllDirectories))
                await processPhpFile(file, projectLocalisationDirectory, reverseMappings);
        }

        private static async Task processPhpFile(string file, string targetDirectory, Dictionary<string, string> languageMappings)
        {
            Console.WriteLine($"Processing {file}...");

            var langParts = Regex.Match(file, @"lang[\/\\]([\w-]+)[\/\\](.*)");

            // The language name - en, ro, etc..
            string langName = langParts.Groups[1].Captures[0].Value;

            // Reverse the language name through the mappings.
            if (languageMappings.TryGetValue(langName, out var mappedName))
                langName = mappedName;

            // Any sub-directories before the php file itself.
            string subDir = Path.GetDirectoryName(langParts.Groups[2].Captures[0].Value) ?? string.Empty;
            subDir = Path.Combine(subDir.Split(Path.DirectorySeparatorChar).Select(d => d.Pascalize()).ToArray());

            // The full namespace, taking into account any sub-directories from above.
            string subNameSpace = subDir.Replace(Path.DirectorySeparatorChar, '.');
            string nameSpace = web_namespace;
            if (!string.IsNullOrEmpty(subNameSpace))
                nameSpace += $".{subNameSpace}";

            // The base name of files generated for this language.
            string name = Path.GetFileNameWithoutExtension(file).Pascalize();
            if (langName != en_lang_name)
                name += $".{langName}";

            // The target directory for files generated for this language.
            targetDirectory = Path.Combine(targetDirectory, subDir);
            Directory.CreateDirectory(targetDirectory); //  Make sure the target directory exists (again, consider sub-directories).

            string targetLocalisationFile = Path.Combine(targetDirectory, $"{name}Strings.cs");
            string targetResourcesFile = Path.Combine(targetDirectory, $"{name}.resx");

            // The localisation members to generate files from.
            var members = (await getMembersFromPhpFile(file)).ToArray();

            if (members.Length == 0)
            {
                Console.WriteLine("Skipped (empty).");
                return;
            }

            // Only create the .cs file for the english localisation.
            if (langName == en_lang_name)
            {
                var localisationFile = new LocalisationFile(nameSpace, Path.GetFileNameWithoutExtension(targetLocalisationFile), name, members);
                using (var fs = File.Open(targetLocalisationFile, FileMode.Create, FileAccess.ReadWrite))
                    await localisationFile.WriteAsync(fs, new AdhocWorkspace());

                Console.WriteLine($"  -> {targetLocalisationFile}");
            }

            // Create the .resx file.
            using (var fs = File.Open(targetResourcesFile, FileMode.Create, FileAccess.ReadWrite))
            using (var resWriter = new ResXResourceWriter(fs, getResourceTypeName))
            {
                foreach (var member in members)
                {
                    if (string.IsNullOrEmpty(member.EnglishText) && langName != en_lang_name)
                        continue;

                    resWriter.AddResource(member.Key, member.EnglishText);
                }

                resWriter.Generate();
            }

            Console.WriteLine($"  -> {targetResourcesFile}");
        }

        private static async Task<IEnumerable<LocalisationMember>> getMembersFromPhpFile(string file)
        {
            // Get the first array from the PHP file.
            string phpContents = await File.ReadAllTextAsync(file);
            int firstBracket = phpContents.IndexOf('[');
            int lastBracket = phpContents.LastIndexOf(']') + 1;
            phpContents = phpContents.Substring(firstBracket, lastBracket - firstBracket);

            return getMembersFromPhpArray(PhpArraySyntaxNode.Parse(new PhpTokeniser(phpContents)));

            static IEnumerable<LocalisationMember> getMembersFromPhpArray(PhpArraySyntaxNode arraySyntax, string? currentKey = null)
            {
                currentKey ??= string.Empty;

                foreach (var i in arraySyntax.Elements)
                {
                    string thisKey = i.Key.Text;
                    string fullKey = $"{currentKey}{thisKey}";

                    switch (i.Value)
                    {
                        case PhpArraySyntaxNode nestedArray:
                            foreach (var nested in getMembersFromPhpArray(nestedArray, $"{fullKey}."))
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
