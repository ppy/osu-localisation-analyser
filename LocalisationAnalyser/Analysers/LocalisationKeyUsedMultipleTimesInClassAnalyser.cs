using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LocalisationAnalyser.Localisation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LocalisationKeyUsedMultipleTimesInClassAnalyser : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticRules.LOCALISATION_KEY_USED_MULTIPLE_TIMES_IN_CLASS);

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxTreeAction(analyseSyntaxTree);
        }

        private void analyseSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            // Optimisation to not inspect too many files.
            if (!context.Tree.FilePath.EndsWith("Strings.cs"))
                return;

            if (!LocalisationFile.TryRead(context.Tree, out var file, out _))
                return;

            var duplicateKeys = findDuplicateKeys(file).ToImmutableHashSet();

            var root = context.Tree.GetRoot();

            foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                markPropertyIfDuplicate(context, property, file, duplicateKeys);

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                markMethodIfDuplicate(context, method, file, duplicateKeys);
        }

        private IEnumerable<string> findDuplicateKeys(LocalisationFile localisationFile)
        {
            var hashSet = new HashSet<string>();

            foreach (var member in localisationFile.Members)
            {
                if (!hashSet.Add(member.Key))
                    yield return member.Key;
            }
        }

        private void markMethodIfDuplicate(SyntaxTreeAnalysisContext context, MethodDeclarationSyntax method,
            LocalisationFile localisationFile, ImmutableHashSet<string> duplicateKeys)
        {
            string? name = method.Identifier.Text;
            if (name == null)
                return;

            var member = localisationFile.Members.SingleOrDefault(m =>
                m.Name == name && m.Parameters.Length == method.ParameterList.Parameters.Count);

            if (member == null)
                return;

            if (!duplicateKeys.Contains(member.Key))
                return;

            var creationExpression = (ObjectCreationExpressionSyntax)method.ExpressionBody.Expression;
            var keyArgument = creationExpression.ArgumentList!.Arguments[0];

            if (keyArgument.Expression is not InvocationExpressionSyntax methodInvocation
                || (methodInvocation.Expression as IdentifierNameSyntax)?.Identifier.Text != "getKey"
                || methodInvocation.ArgumentList.Arguments.Count != 1)
            {
                return;
            }

            var keyString = methodInvocation.ArgumentList.Arguments[0];

            context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.LOCALISATION_KEY_USED_MULTIPLE_TIMES_IN_CLASS, keyString.GetLocation(), member.Key));
        }

        private void markPropertyIfDuplicate(SyntaxTreeAnalysisContext context, PropertyDeclarationSyntax property,
            LocalisationFile localisationFile, ImmutableHashSet<string> duplicateKeys)
        {
            string? name = property.Identifier.Text;
            if (name == null)
                return;

            var member = localisationFile.Members.SingleOrDefault(m => m.Name == name && m.Parameters.Length == 0);

            if (member == null)
                return;

            if (!duplicateKeys.Contains(member.Key))
                return;

            var creationExpression = (ObjectCreationExpressionSyntax)property.ExpressionBody.Expression;
            var keyArgument = creationExpression.ArgumentList!.Arguments[0];

            if (keyArgument.Expression is not InvocationExpressionSyntax methodInvocation
                || (methodInvocation.Expression as IdentifierNameSyntax)?.Identifier.Text != "getKey"
                || methodInvocation.ArgumentList.Arguments.Count != 1)
            {
                return;
            }

            var keyString = methodInvocation.ArgumentList.Arguments[0];

            context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.LOCALISATION_KEY_USED_MULTIPLE_TIMES_IN_CLASS, keyString.GetLocation(), member.Key));
        }
    }
}