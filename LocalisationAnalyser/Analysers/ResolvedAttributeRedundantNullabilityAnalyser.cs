// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ResolvedAttributeRedundantNullabilityAnalyser : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticRules.RESOLVED_ATTRIBUTE_NULLABILITY_IS_REDUNDANT);

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(analyseAttribute, SyntaxKind.Attribute);
        }

        private void analyseAttribute(SyntaxNodeAnalysisContext context)
        {
            // Todo: Use nameof() or equivalent when this project is moved alongside osu-framework.

            AttributeSyntax node = (AttributeSyntax)context.Node;

            // Simple check to avoid processing too many attributes.
            if (node.Name is not IdentifierNameSyntax identifier || !identifier.Identifier.ValueText.StartsWith("Resolved", StringComparison.Ordinal))
                return;

            // Check that the attribute is one of concern.
            AttributeArgumentSyntax[] redundantArgs = node.ArgumentList == null
                ? Array.Empty<AttributeArgumentSyntax>()
                : node.ArgumentList.Arguments
                      .Where(arg =>
                          arg.NameColon?.Name.Identifier.ValueText == "canBeNull" || arg.NameEquals?.Name.Identifier.ValueText == "CanBeNull")
                      .ToArray();

            if (redundantArgs.Length == 0)
                return;

            // Can only eliminate CanBeNull if we're in a nullable context.
            if ((context.SemanticModel.GetNullableContext(node.SpanStart) & NullableContext.Enabled) == 0)
                return;

            // Extended check to ensure the correct attribute is checked.
            var nameSymbol = context.SemanticModel.GetTypeInfo(node.Name);
            if (!nameSymbol.Type.ContainingNamespace.ToString().Contains("osu.Framework.Allocation"))
                return;

            foreach (var arg in redundantArgs)
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.RESOLVED_ATTRIBUTE_NULLABILITY_IS_REDUNDANT, arg.GetLocation(), arg));
        }
    }
}
