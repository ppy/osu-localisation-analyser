// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LocalisationAnalyser.Abstractions.IO;
using LocalisationAnalyser.Localisation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocalisationAnalyser.CodeFixes
{
    internal abstract class AbstractLocaliseStringCodeFixProvider : CodeFixProvider
    {
        private readonly IFileSystem fileSystem;
        private readonly string friendlyLocalisationTarget;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticRules.STRING_CAN_BE_LOCALISED.Id);

        protected AbstractLocaliseStringCodeFixProvider(IFileSystem fileSystem, string friendlyLocalisationTarget)
        {
            this.fileSystem = fileSystem;
            this.friendlyLocalisationTarget = friendlyLocalisationTarget;
        }

        public override FixAllProvider? GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var nodes = root!.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf();

            foreach (var literal in nodes.OfType<LiteralExpressionSyntax>())
            {
                var (_, localisation) = await openOrCreateLocalisation(context.Document.Project, literal);
                var matchingMember = localisation.Members.FirstOrDefault(m => m.EnglishText == literal.Token.ValueText);

                if (matchingMember != null)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            $"Use existing {friendlyLocalisationTarget} localisation: {matchingMember.Name}",
                            c => localiseLiteralAsync(context.Document, literal, c, true),
                            nameof(LocaliseClassStringCodeFixProvider)),
                        diagnostic);
                }
                else
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            $"Add new {friendlyLocalisationTarget} localisation for: {literal}",
                            c => localiseLiteralAsync(context.Document, literal, c, false),
                            nameof(LocaliseClassStringCodeFixProvider)),
                        diagnostic);
                }
            }

            foreach (var interpolated in nodes.OfType<InterpolatedStringExpressionSyntax>())
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Add new {friendlyLocalisationTarget} localisation for: {interpolated}",
                        c => localiseInterpolatedStringAsync(context.Document, interpolated, c),
                        nameof(LocaliseClassStringCodeFixProvider)),
                    diagnostic);
            }
        }

        private async Task<Solution> localiseLiteralAsync(Document document, LiteralExpressionSyntax literal, CancellationToken cancellationToken, bool useExisting)
        {
            return await addMember(
                document,
                literal,
                literal.Token.ValueText,
                Enumerable.Empty<LocalisationParameter>(),
                Enumerable.Empty<ExpressionSyntax>(),
                cancellationToken,
                useExisting);
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

            return await addMember(document,
                interpolated,
                formatString.ToString(),
                paramNames.Select((name, i) => new LocalisationParameter(paramTypes[i], name)),
                paramValues,
                cancellationToken,
                false);
        }

        private async Task<Solution> addMember(Document document, SyntaxNode nodeToReplace, string text, IEnumerable<LocalisationParameter> parameters, IEnumerable<ExpressionSyntax> parameterValues,
                                               CancellationToken cancellationToken, bool useExisting)
        {
            var project = document.Project;
            var solution = project.Solution;

            var (file, localisation) = await openOrCreateLocalisation(project, nodeToReplace);

            MemberAccessExpressionSyntax memberAccess;

            if (!useExisting || !localisation.Members.Any(m => m.EnglishText == text))
            {
                var name = createMemberName(localisation, text);
                var key = name.ToLowerInvariant();
                var newMember = new LocalisationMember(name, key, text, parameters.ToArray());

                localisation = localisation.WithMembers(localisation.Members.Append(newMember).ToArray());

                // Write the resultant localisation class.
                file.FileSystem.Directory.CreateDirectory(file.DirectoryName);
                using (var stream = file.OpenWrite())
                    await localisation.WriteAsync(stream, document.Project.Solution.Workspace);

                memberAccess = LocalisationSyntaxGenerators.GenerateMemberAccessSyntax(localisation, newMember);

                // Check for and add the new class file to the project if required.
                if (project.Solution.Workspace.CanApplyChange(ApplyChangesKind.AddDocument) && project.Documents.All(d => d.FilePath != file.FullName))
                {
                    var classDocument = project.AddDocument(
                        fileSystem.Path.GetFileName(file.FullName),
                        await file.FileSystem.File.ReadAllTextAsync(file.FullName, cancellationToken),
                        Enumerable.Empty<string>(),
                        file.FullName);

                    project = classDocument.Project;
                    solution = project.Solution;
                }
            }
            else
                memberAccess = LocalisationSyntaxGenerators.GenerateMemberAccessSyntax(localisation, localisation.Members.First(m => m.EnglishText == text));

            // Replace the syntax node (the localised string) in the target document.
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)!;
            var newRoot = oldRoot!.ReplaceNode(nodeToReplace, create_syntax_transformation(memberAccess, parameterValues));

            // Check for and add a new using directive to the document if required.
            if (newRoot.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(convertUsingDirectiveToString).All(ud => ud != localisation.Namespace))
            {
                newRoot = ((CompilationUnitSyntax)newRoot)
                    .AddUsings(SyntaxFactory.UsingDirective(
                        SyntaxFactory.ParseName(localisation.Namespace)));
            }

            return solution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }

        private async Task<(IFileInfo, LocalisationFile)> openOrCreateLocalisation(Project project, SyntaxNode sourceNode)
        {
            if (project.FilePath == null)
                throw new ArgumentException("Project cannot have a null path.", nameof(project));

            // Search for the containing class.
            SyntaxNode? containingClass = sourceNode.Parent;
            while (containingClass != null && containingClass.Kind() != SyntaxKind.ClassDeclaration)
                containingClass = containingClass.Parent;
            if (containingClass == null)
                throw new InvalidOperationException("String is not within a class.");

            var projectDirectory = fileSystem.Path.GetDirectoryName(project.FilePath!);
            var localisationDirectory = fileSystem.Path.Combine(new[] { projectDirectory }.Concat(LocalisationSyntaxTemplates.RELATIVE_LOCALISATION_PATH.Split('/')).ToArray());

            // The class being localised.
            var incomingClassName = ((ClassDeclarationSyntax)containingClass).Identifier.Text;

            // The localisation class.
            var localisationNamespace = $"{project.AssemblyName}.{LocalisationSyntaxTemplates.RELATIVE_LOCALISATION_PATH.Replace('/', '.')}";
            var localisationName = GetLocalisationFileName(((ClassDeclarationSyntax)containingClass).Identifier.Text);
            var localisationFileName = fileSystem.Path.Combine(localisationDirectory, fileSystem.Path.ChangeExtension(localisationName, "cs"));
            var localisationFile = fileSystem.FileInfo.FromFileName(localisationFileName);

            if (localisationFile.Exists)
            {
                try
                {
                    using (var stream = localisationFile.OpenRead())
                        return (localisationFile, await LocalisationFile.ReadAsync(stream));
                }
                catch
                {
                }
            }

            return (localisationFile, new LocalisationFile(localisationNamespace, localisationName, GetLocalisationPrefix(incomingClassName)));
        }

        /// <summary>
        /// Retrieves un-namespaced prefix for the localisation class corresponding to a given class name.
        /// </summary>
        /// <param name="className">The name of the original class.</param>
        /// <returns>The un-namespaced prefix.</returns>
        protected virtual string GetLocalisationPrefix(string className) => className;

        /// <summary>
        /// Retrieves the name of the localisation class corresponding to a given class name.
        /// </summary>
        /// <param name="className">The name of the original class.</param>
        /// <returns>The name of the localisation class corresponding to <paramref name="className"/>.</returns>
        protected virtual string GetLocalisationFileName(string className) => className;

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

        private static string createMemberName(LocalisationFile localisation, string englishText)
        {
            var basePropertyName = new string(englishText.Where(char.IsLetter).Take(10).ToArray());
            basePropertyName = char.ToUpperInvariant(basePropertyName[0]) + basePropertyName[1..];

            var finalPropertyName = basePropertyName;
            int propertyNameSuffix = 0;

            while (containsMember(localisation, finalPropertyName))
                finalPropertyName = $"{basePropertyName}{++propertyNameSuffix}";

            return finalPropertyName;

            static bool containsMember(LocalisationFile localisation, string memberName)
                => localisation.Members.Any(m => m.Name == memberName);
        }

        private static string convertUsingDirectiveToString(UsingDirectiveSyntax directive)
        {
            return getName(directive.Name);

            static string getName(NameSyntax name)
            {
                return name switch
                {
                    IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.ToString(),
                    QualifiedNameSyntax qualifiedNameSyntax => $"{getName(qualifiedNameSyntax.Left)}.{getName(qualifiedNameSyntax.Right)}",
                    _ => string.Empty
                };
            }
        }
    }
}
