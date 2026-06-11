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

        protected override void AnalyseMethod(SyntaxTreeAnalysisContext context, MethodDeclarationSyntax method, LocalisationFile localisationFile)
        {
            base.AnalyseMethod(context, method, localisationFile);

            string? name = method.Identifier.Text;
            if (name == null)
                return;

            LocalisationMember member = localisationFile.Members.SingleOrDefault(m => m.Name == name && m.Parameters.Length == method.ParameterList.Parameters.Count);

            if (member == null)
            {
                // Non-localisation member (e.g. getKey()).
                return;
            }

            if (member.EnglishText == member.XmlDoc)
                return;

            if (method.ExpressionBody.Expression is not ObjectCreationExpressionSyntax creationExpression
                || creationExpression.ArgumentList == null)
            {
                return;
            }

            switch (creationExpression.Type.ToString())
            {
                case SyntaxTemplates.PLURALISABLE_STRING_TYPE:
                    if (creationExpression.ArgumentList.Arguments.Count < 1
                        || creationExpression.ArgumentList.Arguments[0].Expression is not ObjectCreationExpressionSyntax innerCreationExpression)
                    {
                        return;
                    }

                    reportError(innerCreationExpression);
                    break;

                case SyntaxTemplates.TRANSLATABLE_STRING_TYPE:
                    reportError(creationExpression);
                    break;
            }

            void reportError(ObjectCreationExpressionSyntax expression)
            {
                if (expression.ArgumentList == null
                    || expression.ArgumentList.Arguments.Count < 2)
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.TEXT_DOES_NOT_MATCH_XMLDOC, expression.ArgumentList.Arguments[1].GetLocation()));
            }
        }

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

            context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.TEXT_DOES_NOT_MATCH_XMLDOC, textArgument.GetLocation()));
        }
    }
}
