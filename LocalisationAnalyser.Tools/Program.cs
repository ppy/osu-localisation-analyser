using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Threading.Tasks;
using LocalisationAnalyser.Localisation;
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

            toResx.Handler = CommandHandler.Create<string>(convertToResX);

            await new RootCommand("osu! Localisation Tools")
            {
                toResx
            }.InvokeAsync(args);
        }

        private static async Task convertToResX(string projectFile)
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
