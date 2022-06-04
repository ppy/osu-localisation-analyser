// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using LocalisationAnalyser.Localisation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocalisationAnalyser.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class MakeXmlDocMatchTextCodeFixProvider : AbstractMemberCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticRules.XMLDOC_DOES_NOT_MATCH_TEXT.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var member = (MemberDeclarationSyntax)root!.FindToken(diagnosticSpan.Start).Parent;

            context.RegisterCodeFix(
                new LocaliseStringCodeAction(
                    "Update XMLDoc to match translation text",
                    (preview, cancellationToken) => UpdateDefinition(context.Document, member, preview, cancellationToken),
                    @"update-xmldoc"),
                diagnostic);
        }

        protected override LocalisationMember FixMember(LocalisationMember member)
            => new LocalisationMember(member.Name, member.Key, member.EnglishText, member.EnglishText, member.Parameters.ToArray());
    }
}
