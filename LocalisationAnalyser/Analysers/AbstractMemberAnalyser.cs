// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using LocalisationAnalyser.Localisation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Analysers
{
    public abstract class AbstractMemberAnalyser : DiagnosticAnalyzer
    {
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

            var root = context.Tree.GetRoot();

            foreach (var prop in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                AnalyseProperty(context, prop, file);
        }

        protected virtual void AnalyseProperty(SyntaxTreeAnalysisContext context, PropertyDeclarationSyntax property, LocalisationFile localisationFile)
        {
        }
    }
}
