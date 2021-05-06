// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LocalisationAnalyser.Abstractions.IO;
using LocalisationAnalyser.Abstractions.IO.Default;
using LocalisationAnalyser.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocalisationAnalyser.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LocalisationCodeFixProvider)), Shared]
    public class LocalisationCodeFixProvider : CodeFixProvider
    {
        private const string localisation_path = "Localisation";
        private const string class_suffix = "Strings";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Analysers.LocalisationAnalyser.DIAGNOSTIC_ID);

        public override FixAllProvider? GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var nodes = root!.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf();

            foreach (var literal in nodes.OfType<LiteralExpressionSyntax>())
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Localise literal string {literal}",
                        c => localiseLiteralAsync(context.Document, literal, c),
                        nameof(LocalisationCodeFixProvider)),
                    diagnostic);
            }

            foreach (var interpolated in nodes.OfType<InterpolatedStringExpressionSyntax>())
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Localise interpolated string {interpolated}",
                        c => localiseInterpolatedStringAsync(context.Document, interpolated, c),
                        nameof(LocalisationCodeFixProvider)),
                    diagnostic);
            }
        }

        private async Task<Solution> localiseLiteralAsync(Document document, LiteralExpressionSyntax literal, CancellationToken cancellationToken)
        {
            return await addMember(document, literal, literal.Token.ValueText, Enumerable.Empty<LocalisationParameter>(), Enumerable.Empty<ExpressionSyntax>(), cancellationToken);
        }

        private async Task<Solution> localiseInterpolatedStringAsync(Document document, InterpolatedStringExpressionSyntax interpolated, CancellationToken cancellationToken)
        {
            var formatString = new StringBuilder();
            var paramNames = new List<string>();
            var paramValues = new List<ExpressionSyntax>();
            var paramTypes = new List<string>();

            int argCount = 0;
            int anonymousArgCount = 0;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
                return document.Project.Solution;

            foreach (var part in interpolated.Contents)
            {
                switch (part)
                {
                    case InterpolatedStringTextSyntax str:
                        formatString.Append(str.TextToken.ValueText);
                        break;

                    case InterpolationSyntax interpolation:
                        formatString.Append($"{{{argCount++}}}");

                        var typeInfo = semanticModel.GetTypeInfo(interpolation.Expression, cancellationToken).Type;
                        if (typeInfo == null)
                            throw new InvalidOperationException("Couldn't determine the type of an interpolation expression.");

                        paramTypes.Add(typeInfo.SpecialType switch
                        {
                            SpecialType.System_Object => "object",
                            SpecialType.System_Boolean => "bool",
                            SpecialType.System_Char => "char",
                            SpecialType.System_SByte => "sbyte",
                            SpecialType.System_Byte => "byte",
                            SpecialType.System_Int16 => "short",
                            SpecialType.System_UInt16 => "ushort",
                            SpecialType.System_Int32 => "int",
                            SpecialType.System_UInt32 => "uint",
                            SpecialType.System_Int64 => "long",
                            SpecialType.System_UInt64 => "ulong",
                            SpecialType.System_Decimal => "decimal",
                            SpecialType.System_Single => "float",
                            SpecialType.System_Double => "double",
                            SpecialType.System_String => "string",
                            _ => typeInfo.Name
                        });

                        if (interpolation.Expression is IdentifierNameSyntax identifier)
                        {
                            // Simple identifier which can be used for both the name and value.
                            paramNames.Add(identifier.Identifier.ValueText);
                            paramValues.Add(identifier);
                            break;
                        }

                        paramNames.Add($"arg{anonymousArgCount++}");

                        if (interpolation.Expression is ParenthesizedExpressionSyntax parens)
                            paramValues.Add(parens.Expression);
                        else
                            paramValues.Add(interpolation.Expression);

                        break;
                }
            }

            return await addMember(document, interpolated, formatString.ToString(), paramNames.Select((name, i) => new LocalisationParameter(paramTypes[i], name)), paramValues, cancellationToken);
        }

        private async Task<Solution> addMember(Document document, SyntaxNode nodeToReplace, string text, IEnumerable<LocalisationParameter> parameters, IEnumerable<ExpressionSyntax> parameterValues,
                                               CancellationToken cancellationToken)
        {
            var project = document.Project;
            var solution = project.Solution;

            var generator = await createGenerator(project, nodeToReplace);

            var name = createMemberName(generator, text);
            var key = char.ToLower(name[0]) + name[1..];

            var memberAccess = generator.AddMember(new LocalisationMember(name, key, text, parameters.ToArray()));
            await generator.Save();

            // Replace the syntax node (the localised string) in the target document.
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot!.ReplaceNode(nodeToReplace, create_syntax_transformation(memberAccess, parameterValues));
            solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);

            // Todo: Check for and add a new using directive to the document if required.

            // Check for and add the new class file to the project if required.
            if (project.Solution.Workspace.CanApplyChange(ApplyChangesKind.AddDocument) && project.Documents.All(d => d.FilePath != generator.ClassFile.FullName))
            {
                var classDocument = project.AddDocument(Path.GetFileName(generator.ClassFile.FullName),
                    await generator.ClassFile.FileSystem.File.ReadAllTextAsync(generator.ClassFile.FullName, cancellationToken),
                    Enumerable.Empty<string>(),
                    generator.ClassFile.FullName);

                project = classDocument.Project;
                solution = project.Solution;
            }

            return solution;
        }

        private async Task<LocalisationClassGenerator> createGenerator(Project project, SyntaxNode sourceNode)
        {
            // Search for the containing class.
            SyntaxNode? containingClass = sourceNode.Parent;
            while (containingClass != null && containingClass.Kind() != SyntaxKind.ClassDeclaration)
                containingClass = containingClass.Parent;
            if (containingClass == null)
                throw new InvalidOperationException("String is not within a class.");

            var fileSystem = GetFileSystem();

            var className = $"{((ClassDeclarationSyntax)containingClass).Identifier.Text}{class_suffix}";

            var solutionDirectory = fileSystem.Path.GetDirectoryName(project.Solution.FilePath)!;
            var projectDirectory = fileSystem.Path.GetDirectoryName(project.FilePath)!;
            var localisationDirectory = fileSystem.Path.Combine(new[] { projectDirectory }.Concat(localisation_path.Split('/')).ToArray());

            var classFileName = fileSystem.Path.Combine(localisationDirectory, fileSystem.Path.ChangeExtension(className, "cs"));
            var classFile = fileSystem.FileInfo.FromFileName(classFileName);
            var classNamespace = localisationDirectory.Replace(solutionDirectory, string.Empty).Replace('/', '.').Trim('.');

            var generator = new LocalisationClassGenerator(project.Solution.Workspace, classFile, classNamespace, className);
            await generator.Open();

            return generator;
        }

        protected virtual IFileSystem GetFileSystem() => new DefaultFileSystem();

        private static SyntaxNode create_syntax_transformation(MemberAccessExpressionSyntax memberAccess, IEnumerable<ExpressionSyntax> parameterValues)
        {
            var valueArray = parameterValues.ToArray();
            if (valueArray.Length == 0)
                return memberAccess;

            return SyntaxFactory.InvocationExpression(memberAccess)
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(
                                            valueArray.Select(SyntaxFactory.Argument))));
        }

        private static string createMemberName(LocalisationClassGenerator generator, string englishText)
        {
            var basePropertyName = new string(englishText.Where(char.IsLetter).Take(10).ToArray());
            var finalPropertyName = basePropertyName;
            int propertyNameSuffix = 0;

            while (containsMember(generator, finalPropertyName))
                finalPropertyName = $"{basePropertyName}{++propertyNameSuffix}";

            return finalPropertyName;

            static bool containsMember(LocalisationClassGenerator generator, string memberName)
                => generator.Members.Any(m => m.Name == memberName);
        }
    }
}
