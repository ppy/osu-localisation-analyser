// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LocalisationAnalyser : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "OLOC001";
        private const string category = "Usage";

        // Disable's roslyn analyser release tracking. Todo: Temporary? The analyser doesn't behave well with Rider :/
        // Read more: https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

#pragma warning disable RS2008

        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            "String can be localised",
            "'{0}' can be localised",
            category,
            DiagnosticSeverity.Info,
            true,
            "Localise string.");

#pragma warning restore RS2008

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(analyseString, SyntaxKind.StringLiteralExpression, SyntaxKind.InterpolatedStringExpression);
        }

        private void analyseString(SyntaxNodeAnalysisContext context)
        {
            switch (context.Node)
            {
                case LiteralExpressionSyntax literal:
                    if (literal.Token.ValueText.Where(char.IsLetter).Any())
                        context.ReportDiagnostic(Diagnostic.Create(rule, context.Node.GetLocation(), context.Node));
                    break;

                case InterpolatedStringExpressionSyntax interpolated:
                    if (interpolated.Contents.Any(c => c is InterpolatedStringTextSyntax text && text.TextToken.ValueText.Where(char.IsLetter).Any()))
                        context.ReportDiagnostic(Diagnostic.Create(rule, context.Node.GetLocation(), context.Node));
                    break;
            }
        }
    }
}
