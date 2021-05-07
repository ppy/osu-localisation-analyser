// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalisationAnalyser.Abstractions.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace LocalisationAnalyser.Generators
{
    /// <summary>
    /// A generator for the localisation class, containing localisable strings as static properties and methods.
    /// </summary>
    public class LocalisationClassGenerator
    {
        /// <summary>
        /// All members part of the class.
        /// </summary>
        public ImmutableArray<LocalisationMember> Members => members.ToImmutableArray();

        private readonly ImmutableArray<LocalisationMember>.Builder members = ImmutableArray.CreateBuilder<LocalisationMember>();

        public readonly IFileInfo ClassFile;
        public readonly string ClassNamespace;
        public readonly string ClassName;
        private readonly Workspace workspace;
        private readonly string prefix;

        /// <summary>
        /// Creates a new localisation class generator.
        /// </summary>
        /// <param name="workspace">The generation workspace, used for code formatting.</param>
        /// <param name="classFile">The localisation class file.</param>
        /// <param name="classNamespace">The localisation class namespace.</param>
        /// <param name="className">The localisation class name.</param>
        /// <param name="prefix">The localisation prefix.</param>
        public LocalisationClassGenerator(Workspace workspace, IFileInfo classFile, string classNamespace, string className, string prefix)
        {
            this.workspace = workspace;
            ClassFile = classFile;
            ClassNamespace = classNamespace;
            ClassName = className;
            this.prefix = prefix;
        }

        /// <summary>
        /// Opens the localisation class, or creates a new one if one doesn't already exist.
        /// </summary>
        public async Task Open()
        {
            if (!ClassFile.Exists)
                return;

            using (var sr = new StreamReader(ClassFile.OpenRead()))
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(await sr.ReadToEndAsync());
                var syntaxRoot = await syntaxTree.GetRootAsync();

                var classSyntax = syntaxRoot.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().SingleOrDefault(c => c.Identifier.ToString() == ClassName);

                if (classSyntax == null)
                    return;

                var walker = new LocalisationClassWalker();
                classSyntax.Accept(walker);
                members.AddRange(walker.Members);
            }
        }

        /// <summary>
        /// Generates and saves the localisation class.
        /// </summary>
        public async Task Save()
        {
            ClassFile.FileSystem.Directory.CreateDirectory(ClassFile.DirectoryName!);

            using (var sw = new StreamWriter(ClassFile.OpenWrite()))
                await sw.WriteAsync(Formatter.Format(generateClassSyntax(), workspace).ToFullString());
        }

        /// <summary>
        /// Adds a new member to the localisation class.
        /// </summary>
        /// <param name="member">The member to add.</param>
        /// <returns>A <see cref="MemberAccessExpressionSyntax"/> that can be used to refer to the added member.</returns>
        public MemberAccessExpressionSyntax AddMember(LocalisationMember member)
        {
            members.Add(member);
            return GenerateMemberAccessSyntax(member);
        }

        /// <summary>
        /// Removes a member from the localisation class.
        /// </summary>
        /// <param name="member">The member to remove.</param>
        public void RemoveMember(LocalisationMember member)
        {
            members.Remove(member);
        }

        /// <summary>
        /// Generates the full class syntax, including the namespace, all leading/trailing members, and the localisation members.
        /// </summary>
        /// <returns>The syntax.</returns>
        private SyntaxNode generateClassSyntax()
            => SyntaxFactory.ParseCompilationUnit(LocalisationClassTemplates.FILE_HEADER_SIGNATURE)
                            .AddMembers(
                                SyntaxFactory.NamespaceDeclaration(
                                                 SyntaxFactory.IdentifierName(ClassNamespace))
                                             .WithMembers(
                                                 SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                                     SyntaxFactory.ClassDeclaration(ClassName)
                                                                  .WithMembers(SyntaxFactory.List(
                                                                      Members
                                                                          .Select(m => m.Parameters.Length == 0 ? generatePropertySyntax(m) : generateMethodSyntax(m))
                                                                          .Prepend(generatePrefixSyntax())
                                                                          .Append(generateGetKeySyntax())))
                                                                  .WithModifiers(
                                                                      new SyntaxTokenList(
                                                                          SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                                          SyntaxFactory.Token(SyntaxKind.StaticKeyword))))));

        /// <summary>
        /// Generates the syntax for a property member.
        /// </summary>
        private MemberDeclarationSyntax generatePropertySyntax(LocalisationMember member)
            => SyntaxFactory.ParseMemberDeclaration(
                string.Format(LocalisationClassTemplates.PROPERTY_SIGNATURE,
                    member.Name,
                    member.Key,
                    convertToVerbatim(member.EnglishText),
                    member.EnglishText))!;

        /// <summary>
        /// Generates the syntax for a method member.
        /// </summary>
        private MemberDeclarationSyntax generateMethodSyntax(LocalisationMember member)
        {
            var paramList = SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    member.Parameters.Select(param => SyntaxFactory.Parameter(
                                                                       SyntaxFactory.Identifier(param.Name))
                                                                   .WithType(
                                                                       SyntaxFactory.IdentifierName(param.Type)))));

            var argList = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    member.Parameters.Select(param => SyntaxFactory.Argument(
                        SyntaxFactory.IdentifierName(param.Name)))));

            return SyntaxFactory.ParseMemberDeclaration(
                string.Format(LocalisationClassTemplates.METHOD_SIGNATURE,
                    member.Name,
                    Formatter.Format(paramList, workspace).ToFullString(),
                    member.Key,
                    convertToVerbatim(member.EnglishText),
                    Formatter.Format(argList, workspace).ToFullString()[1..^1], // The entire string minus the parens
                    member.EnglishText))!; // Todo: Improve xmldoc
        }

        /// <summary>
        /// Generates the syntax for accessing the member.
        /// </summary>
        public MemberAccessExpressionSyntax GenerateMemberAccessSyntax(LocalisationMember member)
            => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(ClassName),
                SyntaxFactory.IdentifierName(member.Name));

        /// <summary>
        /// Generates the syntax for the prefix constant.
        /// </summary>
        private MemberDeclarationSyntax generatePrefixSyntax()
            => SyntaxFactory.ParseMemberDeclaration(string.Format(LocalisationClassTemplates.PREFIX_SIGNATURE, $"{ClassNamespace}.{prefix}"))!;

        /// <summary>
        /// Generates the syntax for the getKey() method.
        /// </summary>
        private MemberDeclarationSyntax generateGetKeySyntax()
            => SyntaxFactory.ParseMemberDeclaration(LocalisationClassTemplates.GET_KEY_SIGNATURE)!;

        /// <summary>
        /// Converts a string literal to its verbatim representation. Assumes that the string is already non-verbatim.
        /// </summary>
        /// <param name="input">The non-verbatim string.</param>
        /// <returns>The verbatim replacement.</returns>
        private static string convertToVerbatim(string input)
        {
            var result = new StringBuilder();

            foreach (var c in input)
            {
                result.Append(c);

                if (c == '"')
                    result.Append(c);
            }

            return result.ToString();
        }
    }
}
