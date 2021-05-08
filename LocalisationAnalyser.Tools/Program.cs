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

            var classFiles = project.Documents.Where(d => d.Folders.FirstOrDefault() == SyntaxTemplates.PROJECT_RELATIVE_LOCALISATION_PATH)
                                    .Where(d => d.Name.EndsWith(".cs"))
                                    .Where(d => d.Name[..^3].Count(c => c == '.') == 0)
                                    .ToArray();

            if (classFiles.Length == 0)
            {
                Console.WriteLine("No localisation files found in project.");
                return;
            }

            foreach (var file in classFiles)
            {
                Console.WriteLine($"Processing {file.Name}...");

                string resxFile = Path.ChangeExtension(file.FilePath, "resx");

                LocalisationFile localisationFile;
                using (var stream = File.OpenRead(file.FilePath))
                    localisationFile = await LocalisationFile.ReadAsync(stream);

                using (var fs = File.Open(resxFile, FileMode.Create, FileAccess.ReadWrite))
                using (var resWriter = new ResXResourceWriter(fs))
                {
                    foreach (var member in localisationFile.Members)
                        resWriter.AddResource($"{localisationFile.Prefix}:{member.Key}", member.EnglishText);
                    resWriter.Generate();
                }

                Console.WriteLine($"  -> {resxFile}");
            }
        }
    }
}
