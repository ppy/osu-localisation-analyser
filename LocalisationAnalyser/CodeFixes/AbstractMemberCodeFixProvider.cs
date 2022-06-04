// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LocalisationAnalyser.Localisation;
using LocalisationAnalyser.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LocalisationAnalyser.CodeFixes
{
    public abstract class AbstractMemberCodeFixProvider : CodeFixProvider
    {
        protected async Task<Solution> UpdateDefinition(Document document, MemberDeclarationSyntax member, bool preview, CancellationToken cancellationToken)
        {
            LocalisationFile currentFile;

            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
                    await sw.WriteAsync((await document.GetTextAsync(cancellationToken)).ToString());

                ms.Seek(0, SeekOrigin.Begin);

                currentFile = await LocalisationFile.ReadAsync(ms);
            }

            LocalisationFile updatedFile = currentFile.WithMembers(currentFile.Members.Select(m =>
            {
                if (m.Name != ((PropertyDeclarationSyntax)member).Identifier.Text)
                    return m;

                return FixMember(m);
            }).ToArray());

            using (var ms = new MemoryStream())
            {
                var options = await document.GetAnalyserOptionsAsync(cancellationToken);
                await updatedFile.WriteAsync(ms, document.Project.Solution.Workspace, options, true);

                return document.WithText(SourceText.From(ms, Encoding.UTF8)).Project.Solution;
            }
        }

        protected abstract LocalisationMember FixMember(LocalisationMember member);
    }
}
