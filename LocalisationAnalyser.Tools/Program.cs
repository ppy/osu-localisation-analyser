using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Threading.Tasks;
using LocalisationAnalyser.Abstractions.IO.Default;
using LocalisationAnalyser.CodeFixes;
using LocalisationAnalyser.Generators;
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
            MSBuildLocator.RegisterDefaults();

            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(projectFile);

            var classFiles = project.Documents.Where(d => d.Folders.FirstOrDefault() == AbstractLocaliseStringCodeFixProvider.RELATIVE_LOCALISATION_PATH)
                                    .Where(d => d.Name.EndsWith(".cs"))
                                    .Where(d => d.Name[..^3].Count(c => c == '.') == 0);

            foreach (var classFile in classFiles)
            {
                string className = Path.GetFileNameWithoutExtension(classFile.Name)!;
                string classNamespace = $"{project.AssemblyName}.{AbstractLocaliseStringCodeFixProvider.RELATIVE_LOCALISATION_PATH}";
                string classPrefix = $"{classNamespace}.{className}";

                var generator = new LocalisationClassGenerator(
                    workspace,
                    new DefaultFileSystem().FileInfo.FromFileName(classFile.FilePath),
                    classNamespace,
                    className,
                    classPrefix);

                await generator.Open();

                using (var ms = new MemoryStream())
                using (var resWriter = new ResXResourceWriter(ms))
                {
                    foreach (var member in generator.Members)
                        resWriter.AddResource($"{classPrefix}:{member.Key}", member.EnglishText);
                    resWriter.Generate();

                    using (var fs = File.Open(Path.ChangeExtension(classFile.FilePath, "resx"), FileMode.Create, FileAccess.ReadWrite))
                        ms.WriteTo(fs);
                }
            }
        }
    }
}
