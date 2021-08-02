// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Utils
{
    public static class DocumentUtils
    {
        public static async Task<AnalyzerConfigOptions> GetAnalyserOptionsAsync(this Document document, CancellationToken cancellationToken)
        {
            var project = document.Project;

            var analyzersInAdditionalDocuments = project.AdditionalDocuments
                                                        .Where(d => d.FilePath.EndsWith(".editorconfig"))
                                                        .Select(d => d.Id);

            // Rider <= 2021.2 EAP5 puts analyzer configs in the incorrect location (project.AdditionalDocuments rather than the expected project.AnalyzerConfigDocuments).
            // We need to manually duplicate these files into AnalyzerConfigDocuments to allow the analyser to read the file.
            // Note that, also due to Rider, this only handles the project's .editorconfig file.
            // Todo: Remove when https://youtrack.jetbrains.com/issue/RIDER-64877 is fixed!!
            foreach (var docId in analyzersInAdditionalDocuments)
            {
                var doc = project.GetAdditionalDocument(docId);
                var text = await doc!.GetTextAsync(cancellationToken);
                project = project.AddAnalyzerConfigDocument(doc.Name, text, doc.Folders, doc.FilePath).Project;
            }

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var options = project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
            return options;
        }
    }
}
