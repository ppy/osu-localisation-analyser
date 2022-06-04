// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.Linq;
using LocalisationAnalyser.Localisation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TextDoesNotMatchXmlDocAnalyser : AbstractMemberAnalyser
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticRules.TEXT_DOES_NOT_MATCH_XMLDOC);

        protected override void AnalyseProperty(SyntaxTreeAnalysisContext context, PropertyDeclarationSyntax property, LocalisationFile localisationFile)
        {
            base.AnalyseProperty(context, property, localisationFile);

            string? name = property.Identifier.Text;
            if (name == null)
                return;

            LocalisationMember member = localisationFile.Members.Single(m => m.Name == name && m.Parameters.Length == 0);

            if (member.EnglishText == member.XmlDoc)
                return;

            var creationExpression = (ObjectCreationExpressionSyntax)property.ExpressionBody.Expression;
            var textArgument = creationExpression.ArgumentList!.Arguments.Last();

            context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.TEXT_DOES_NOT_MATCH_XMLDOC, textArgument.GetLocation(), property));
        }
    }
}
