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
using LocalisationAnalyser.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace LocalisationAnalyser.CodeFixes
{
    /// <summary>
    /// Abstract code-fix provider for <see cref="DiagnosticRules.STRING_CAN_BE_LOCALISED"/> inspections to replace strings with a localisation.
    /// </summary>
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
                var (_, localisation) = await openOrCreateLocalisation(context.Document.Project, literal, null);
                var matchingMember = localisation.Members.FirstOrDefault(m => m.EnglishText == literal.Token.ValueText);

                if (matchingMember != null)
                {
                    context.RegisterCodeFix(
                        new LocaliseStringCodeAction(
                            $"Use existing {friendlyLocalisationTarget} localisation: {matchingMember.Name}",
                            (preview, cancellationToken) => localiseLiteralAsync(context.Document, literal, preview, cancellationToken, true),
                            $@"{friendlyLocalisationTarget}-existing-literal"),
                        diagnostic);
                }
                else
                {
                    context.RegisterCodeFix(
                        new LocaliseStringCodeAction(
                            $"Add new {friendlyLocalisationTarget} localisation for: {literal}",
                            (preview, cancellationToken) => localiseLiteralAsync(context.Document, literal, preview, cancellationToken, false),
                            $@"{friendlyLocalisationTarget}-new-literal"),
                        diagnostic);
                }
            }

            foreach (var interpolated in nodes.OfType<InterpolatedStringExpressionSyntax>())
            {
                context.RegisterCodeFix(
                    new LocaliseStringCodeAction(
                        $"Add new {friendlyLocalisationTarget} localisation for: {interpolated}",
                        (preview, cancellationToken) => localiseInterpolatedStringAsync(context.Document, interpolated, preview, cancellationToken),
                        $@"{friendlyLocalisationTarget}-new-interpolated"),
                    diagnostic);
            }
        }

        private async Task<Solution> localiseLiteralAsync(Document document, LiteralExpressionSyntax literal, bool preview, CancellationToken cancellationToken, bool useExisting)
        {
            return await addMember(
                document,
                literal,
                literal.Token.ValueText,
                Enumerable.Empty<LocalisationParameter>(),
                Enumerable.Empty<ExpressionSyntax>(),
                preview,
                cancellationToken,
                useExisting);
        }

        private async Task<Solution> localiseInterpolatedStringAsync(Document document, InterpolatedStringExpressionSyntax interpolated, bool preview, CancellationToken cancellationToken)
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
                preview,
                cancellationToken,
                false);
        }

        private async Task<Solution> addMember(Document document, SyntaxNode nodeToReplace, string text, IEnumerable<LocalisationParameter> parameters, IEnumerable<ExpressionSyntax> parameterValues,
                                               bool preview, CancellationToken cancellationToken, bool useExisting)
        {
            var project = document.Project;
            var solution = project.Solution;

            var options = await document.GetAnalyserOptionsAsync(cancellationToken);
            var (file, localisation) = await openOrCreateLocalisation(project, nodeToReplace, options);

            MemberAccessExpressionSyntax memberAccess;

            if (!useExisting || !localisation.Members.Any(m => m.EnglishText == text))
            {
                var name = createMemberName(localisation, text, options);
                var key = createKey(name);
                var newMember = new LocalisationMember(name, key, text, parameters.ToArray());

                localisation = localisation.WithMembers(localisation.Members.Append(newMember).ToArray());

                if (!preview)
                {
                    // Write the resultant localisation class.
                    file.FileSystem.Directory.CreateDirectory(file.DirectoryName);
                    using (var stream = file.OpenWrite())
                        await localisation.WriteAsync(stream, document.Project.Solution.Workspace, options);

                    // Check for and add the new class file to the project if required.
                    if (project.Solution.Workspace.CanApplyChange(ApplyChangesKind.AddDocument) && project.Documents.All(d => d.FilePath != file.FullName))
                    {
                        var classDocument = project.AddDocument(
                            file.FullName,
                            SourceText.From(file.FileSystem.File.ReadAllText(file.FullName), Encoding.UTF8),
                            Enumerable.Empty<string>(),
                            file.FullName);

                        project = classDocument.Project;
                        solution = project.Solution;
                    }
                }

                memberAccess = SyntaxGenerators.GenerateMemberAccessSyntax(localisation, newMember);
            }
            else
                memberAccess = SyntaxGenerators.GenerateMemberAccessSyntax(localisation, localisation.Members.First(m => m.EnglishText == text));

            // Replace the syntax node (the localised string) in the target document.
            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (nodeToReplace.Parent.Kind() == SyntaxKind.AttributeArgument)
            {
                rootNode = rootNode.ReplaceNode(nodeToReplace.FirstAncestorOrSelf<AttributeSyntax>(), SyntaxGenerators.GenerateAttributeAccessSyntax(memberAccess));
                rootNode = SyntaxGenerators.AddUsingDirectiveIfNotExisting((CompilationUnitSyntax)rootNode, SyntaxTemplates.FRAMEWORK_LOCALISATION_NAMESPACE);
            }
            else
                rootNode = rootNode.ReplaceNode(nodeToReplace, SyntaxGenerators.GenerateDirectAccessSyntax(memberAccess, parameterValues));

            rootNode = SyntaxGenerators.AddUsingDirectiveIfNotExisting((CompilationUnitSyntax)rootNode, localisation.Namespace);

            return solution.WithDocumentSyntaxRoot(document.Id, rootNode);
        }

        private async Task<(IFileInfo, LocalisationFile)> openOrCreateLocalisation(Project project, SyntaxNode sourceNode, AnalyzerConfigOptions? options)
        {
            if (project.FilePath == null)
                throw new ArgumentException("Project cannot have a null path.", nameof(project));

            // Search for the top-level containing class.
            BaseTypeDeclarationSyntax? containingType = null;
            SyntaxNode? parentNode = sourceNode.Parent;

            while (parentNode != null)
            {
                if (parentNode.Kind() == SyntaxKind.ClassDeclaration || parentNode.Kind() == SyntaxKind.StructDeclaration || parentNode.Kind() == SyntaxKind.EnumDeclaration)
                    containingType = (BaseTypeDeclarationSyntax)parentNode;
                parentNode = parentNode.Parent;
            }

            if (containingType == null)
                throw new InvalidOperationException("String is not within a class, struct, or enum type.");

            var projectDirectory = fileSystem.Path.GetDirectoryName(project.FilePath!);
            var localisationDirectory = fileSystem.Path.Combine(new[] { projectDirectory }.Concat(SyntaxTemplates.PROJECT_RELATIVE_LOCALISATION_PATH.Split('/')).ToArray());

            // The class being localised.
            var incomingClassName = containingType.Identifier.Text;

            // The localisation class.
            var localisationNamespace = $"{project.AssemblyName}.{SyntaxTemplates.PROJECT_RELATIVE_LOCALISATION_PATH.Replace('/', '.')}";
            var localisationName = GetLocalisationFileName(containingType.Identifier.Text);
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

            // The prefix namespace defaults to the localisation class' namespace, but can be customised via .editorconfig.
            var prefixNamespace = localisationNamespace;

            if (options != null && options.TryGetValue($"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.prefix_namespace", out var customPrefixNamespace))
            {
                if (string.IsNullOrEmpty(customPrefixNamespace))
                    throw new InvalidOperationException("Custom namespace cannot be empty.");

                prefixNamespace = customPrefixNamespace;
            }

            return (localisationFile, new LocalisationFile(localisationNamespace, localisationName, GetLocalisationPrefix(prefixNamespace, incomingClassName)));
        }

        /// <summary>
        /// Retrieves "prefix" value for the localisation class corresponding to a given class name and namespace.
        /// </summary>
        /// <param name="namespace">The namespace in which the localisation class will be placed.</param>
        /// <param name="className">The name of the original class.</param>
        /// <returns>The prefix value.</returns>
        protected virtual string GetLocalisationPrefix(string @namespace, string className) => $"{@namespace}.{className}";

        /// <summary>
        /// Retrieves the name of the localisation class corresponding to a given class name.
        /// </summary>
        /// <param name="className">The name of the original class.</param>
        /// <returns>The name of the localisation class corresponding to <paramref name="className"/>.</returns>
        protected virtual string GetLocalisationFileName(string className) => className;

        private static string createMemberName(LocalisationFile localisation, string englishText, AnalyzerConfigOptions? options)
        {
            var basePropertyNameBuilder = new StringBuilder();

            int wordsInName = int.MaxValue;

            if (options != null && options.TryGetValue($"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.words_in_name", out var customWordsInName))
            {
                if (!int.TryParse(customWordsInName, out wordsInName))
                    wordsInName = int.MaxValue;
            }

            foreach (var word in englishText.Split(' '))
            {
                bool first = true;
                bool anyAdded = false;

                for (int i = 0; i < word.Length; i++)
                {
                    if (!char.IsLetter(word[i]))
                    {
                        // Special case for apostrophes to not start new words.
                        if (word[i] != '\'')
                            first = true;
                        continue;
                    }

                    basePropertyNameBuilder.Append(first ? char.ToUpperInvariant(word[i]) : word[i]);

                    anyAdded = true;
                    first = false;
                }

                if (anyAdded && --wordsInName == 0)
                    break;
            }

            var basePropertyName = basePropertyNameBuilder.ToString();
            var finalPropertyName = basePropertyName;
            int propertyNameSuffix = 0;

            while (containsMember(localisation, finalPropertyName))
                finalPropertyName = $"{basePropertyName}{++propertyNameSuffix}";

            return finalPropertyName;

            static bool containsMember(LocalisationFile localisation, string memberName)
                => localisation.Members.Any(m => m.Name == memberName);
        }

        private static string createKey(string memberName)
        {
            var keyBuilder = new StringBuilder();

            foreach (var c in memberName)
            {
                if (char.IsUpper(c) && keyBuilder.Length > 0)
                    keyBuilder.Append('_');
                keyBuilder.Append(char.ToLowerInvariant(c));
            }

            return keyBuilder.ToString();
        }
    }
}
