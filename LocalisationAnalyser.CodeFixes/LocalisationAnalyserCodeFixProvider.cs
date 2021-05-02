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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace LocalisationAnalyser.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LocalisationAnalyserCodeFixProvider)), Shared]
    public class LocalisationAnalyserCodeFixProvider : CodeFixProvider
    {
        private const string localisation_path = "LocalisationAnalyser.Tests/Localisation";
        private const string class_suffix = "Strings";
        private static readonly string localisation_namespace = localisation_path.Replace('/', '.');

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(LocalisationAnalyser.DIAGNOSTIC_ID);

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
                        nameof(LocalisationAnalyserCodeFixProvider)),
                    diagnostic);
            }

            foreach (var interpolated in nodes.OfType<InterpolatedStringExpressionSyntax>())
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Localise interpolated string {interpolated}",
                        c => localiseInterpolatedStringAsync(context.Document, interpolated, c),
                        nameof(LocalisationAnalyserCodeFixProvider)),
                    diagnostic);
            }

            // foreach (var op in nodes.OfType<BinaryExpressionSyntax>())
            // {
            //     context.RegisterCodeFix(
            //         CodeAction.Create(
            //             "Localise string operation",
            //             c => Task.FromResult(context.Document),
            //             nameof(LocalisationAnalyserCodeFixProvider)),
            //         diagnostic);
            // }
        }

        private async Task<Document> localiseLiteralAsync(Document document, LiteralExpressionSyntax literal, CancellationToken cancellationToken)
        {
            var localisation = await getOrCreateLocalisation(document.Project, literal);
            var member = create_property_member(localisation, literal);

            localisation = insertMember(localisation, member);

            // Save the new localisation document syntax.
            await saveLocalisation(document.Project, localisation);

            var newNode = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(localisation.Identifier.ValueText),
                SyntaxFactory.IdentifierName(member.Identifier.ValueText));

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot!.ReplaceNode(literal, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> localiseInterpolatedStringAsync(Document document, InterpolatedStringExpressionSyntax interpolated, CancellationToken cancellationToken)
        {
            try
            {
                var formatString = new StringBuilder();
                var paramNames = new List<string>();
                var paramValues = new List<ExpressionSyntax>();

                int argCount = 0;
                int anonymousArgCount = 0;

                foreach (var part in interpolated.Contents)
                {
                    switch (part)
                    {
                        case InterpolatedStringTextSyntax str:
                            formatString.Append(str.TextToken.ValueText);
                            break;

                        case InterpolationSyntax interpolation:
                            formatString.Append($"{{{argCount++}}}");

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

                var localisation = await getOrCreateLocalisation(document.Project, interpolated);
                var member = create_method_member(document.Project.Solution.Workspace, localisation, paramNames.ToArray(), formatString.ToString());

                localisation = insertMember(localisation, member);

                // Save the new localisation document syntax.
                await saveLocalisation(document.Project, localisation);

                var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = oldRoot!.ReplaceNode(interpolated, create_method_transformation(localisation, member, paramValues));

                return document.WithSyntaxRoot(newRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText("/home/smgi/Desktop/test.txt", ex.ToString());
                throw;
            }
        }

        private async Task<ClassDeclarationSyntax> getOrCreateLocalisation(Project project, SyntaxNode sourceNode)
        {
            // Search for the containing class.
            SyntaxNode? containingClass = sourceNode.Parent;
            while (containingClass != null && containingClass.Kind() != SyntaxKind.ClassDeclaration)
                containingClass = containingClass.Parent;

            if (containingClass == null)
                throw new InvalidOperationException("String is not within a class.");

            // Create the syntax for the localisation document.
            var className = $"{((ClassDeclarationSyntax)containingClass).Identifier.Text}{class_suffix}";
            var fileName = getLocalisationFileName(project, className);

            if (File.Exists(fileName))
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(fileName));
                var syntaxRoot = await syntaxTree.GetRootAsync();
                var classSyntax = syntaxRoot.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().SingleOrDefault();

                if (classSyntax != null)
                    return classSyntax;
            }

            return SyntaxFactory.ClassDeclaration(className)
                                .WithMembers(SyntaxFactory.List(new[]
                                {
                                    create_prefix(className),
                                    create_get_key()
                                }));
        }

        private ClassDeclarationSyntax insertMember(ClassDeclarationSyntax classSyntax, MemberDeclarationSyntax member)
        {
            // Find index of the "getKey" element.
            MethodDeclarationSyntax getKeySyntax = (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(get_key_template)!;
            MethodDeclarationSyntax getKeyMember = classSyntax.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == getKeySyntax.Identifier.ValueText)!;

            return classSyntax.WithMembers(
                classSyntax.Members.Insert(
                    classSyntax.Members.IndexOf(getKeyMember),
                    member));
        }

        private async Task saveLocalisation(Project project, ClassDeclarationSyntax classSyntax)
        {
            var fileName = getLocalisationFileName(project, classSyntax.Identifier.ValueText);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);

            var namespacedSyntax = SyntaxFactory.NamespaceDeclaration(
                                                    SyntaxFactory.IdentifierName(localisation_namespace))
                                                .WithMembers(
                                                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                                        classSyntax.WithMembers(
                                                            SyntaxFactory.List(
                                                                classSyntax.Members.Select(m => m.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))))));

            // OptionSet options = project.Solution.Workspace.Options;
            // options = options.WithChangedOption(CSharpFormattingOptions.IndentBlock, true);
            // options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, true);
            // options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);

            await File.WriteAllTextAsync(fileName, Formatter.Format(namespacedSyntax, project.Solution.Workspace).ToFullString());
        }

        private string getLocalisationFileName(Project project, string className)
        {
            var solutionDirectory = Path.GetDirectoryName(project.Solution.FilePath);
            var localisationDirectory = Path.Combine(new[] { solutionDirectory! }.Concat(localisation_path.Split('/')).ToArray());

            return Path.Combine(localisationDirectory, Path.ChangeExtension(className, "cs"));
        }

        private static MemberDeclarationSyntax create_prefix(string className)
            => SyntaxFactory.ParseMemberDeclaration(string.Format(prefix_template, $"{localisation_namespace}.{className}"))!;

        private static PropertyDeclarationSyntax create_property_member(ClassDeclarationSyntax classSyntax, LiteralExpressionSyntax literal)
        {
            string literalText = literal.Token.ValueText;

            string keyName = getDefaultKeyName(classSyntax, literalText);
            string memberName = char.ToUpper(keyName[0]) + keyName[1..];

            return (PropertyDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
                string.Format(property_member_template,
                    memberName,
                    keyName,
                    literalText,
                    literalText))!;
        }

        private static MethodDeclarationSyntax create_method_member(Workspace workspace, ClassDeclarationSyntax classSyntax, string[] paramNames, string englishText)
        {
            var paramList = SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    paramNames.Select(arg => SyntaxFactory.Parameter(
                                                              SyntaxFactory.Identifier(arg))
                                                          .WithType(
                                                              SyntaxFactory.PredefinedType(
                                                                  SyntaxFactory.Token(SyntaxKind.StringKeyword))))));

            string keyName = getDefaultKeyName(classSyntax, englishText);
            string memberName = char.ToUpper(keyName[0]) + keyName[1..];

            return (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
                string.Format(method_member_template,
                    memberName,
                    Formatter.Format(paramList, workspace).ToFullString(),
                    keyName,
                    englishText,
                    englishText))!; // Todo: Improve xmldoc
        }

        private static InvocationExpressionSyntax create_method_transformation(ClassDeclarationSyntax localisation, MethodDeclarationSyntax method, IEnumerable<ExpressionSyntax> paramValues)
            => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(localisation.Identifier.ValueText),
                                    SyntaxFactory.IdentifierName(method.Identifier.ValueText)))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        paramValues.Select(SyntaxFactory.Argument))));

        private static MemberDeclarationSyntax create_get_key()
            => SyntaxFactory.ParseMemberDeclaration(get_key_template)!;

        private static string getDefaultKeyName(ClassDeclarationSyntax classSyntax, string englishText)
        {
            var basePropertyName = new string(englishText.Where(char.IsLetter).Take(10).ToArray());
            var finalPropertyName = basePropertyName;
            int propertyNameSuffix = 0;

            while (classContainsMember(classSyntax, finalPropertyName))
                finalPropertyName = $"{basePropertyName}{++propertyNameSuffix}";

            return finalPropertyName;

            static bool classContainsMember(ClassDeclarationSyntax classSyntax, string memberName)
                => classSyntax.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == memberName)
                   || classSyntax.Members.OfType<MethodDeclarationSyntax>().Any(p => p.Identifier.Text == memberName);
        }

        /// <summary>
        /// 0 -> Value
        /// </summary>
        private const string prefix_template = @"
private const string prefix = ""{0}"";";

        /// <summary>
        /// 0 -> Member name
        /// 1 -> Key
        /// 2 -> English text
        /// 3 -> Xmldoc
        /// </summary>
        private const string property_member_template = @"
/// <summary>
/// ""{3}""
/// </summary>
public static LocalisableString {0} => new TranslatableString(getKey(""{1}""), ""{2}"");";

        /// <summary>
        /// 0 -> Member name
        /// 1 -> Parameters
        /// 2 -> Key
        /// 3 -> English text
        /// 4 -> Xmldoc
        /// </summary>
        private const string method_member_template = @"
/// <summary>
/// ""{4}""
/// </summary>
public static LocalisableString {0}{1} => new TranslatableString(getKey(""{2}""), ""{3}"");";

        private const string get_key_template = @"
private static string getKey(string key) => $""{prefix}:{key}"";";
    }
}
