// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LocalisationAnalyser
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LocalisationAnalyser : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "Localisation";
        private const string category = "Usage";

        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            "String can be localised",
            "'{0}' can be localised",
            category,
            DiagnosticSeverity.Info,
            true,
            "Localise string.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(analyseOperation, OperationKind.BinaryOperator);
            context.RegisterSyntaxNodeAction(analyseString, SyntaxKind.StringLiteralExpression, SyntaxKind.InterpolatedStringExpression);
        }

        private void analyseOperation(OperationAnalysisContext context)
        {
            if (IsAddOperationOnString((IBinaryOperation)context.Operation))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, context.Operation.Syntax.GetLocation(), ""));
            }
        }

        private void analyseString(SyntaxNodeAnalysisContext context)
        {
            // SyntaxNode? parentNode = context.Node;
            //
            // while ((parentNode = parentNode.Parent) != null)
            // {
            //     if (parentNode.Kind() == SyntaxKind.AddExpression)
            //         return;
            // }

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

        public static bool IsAddOperationOnString(IBinaryOperation operation)
        {
            if (operation.OperatorKind != BinaryOperatorKind.Add)
                return false;

            if (operation.LeftOperand.Type?.SpecialType == SpecialType.System_String
                || operation.RightOperand.Type?.SpecialType == SpecialType.System_String)
            {
                return true;
            }

            return operation.Children.OfType<IBinaryOperation>().Any(IsAddOperationOnString);
        }
    }
}
