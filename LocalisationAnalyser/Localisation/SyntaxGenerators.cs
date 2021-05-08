// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace LocalisationAnalyser.Localisation
{
    /// <summary>
    /// <see cref="SyntaxNode"/> generators for use with a <see cref="LocalisationFile"/>.
    /// </summary>
    internal static class SyntaxGenerators
    {
        /// <summary>
        /// Generates the full class syntax, including the namespace, all leading/trailing members, and the localisation members.
        /// </summary>
        /// <returns>The syntax.</returns>
        public static SyntaxNode GenerateClassSyntax(Workspace workspace, LocalisationFile localisationFile)
            => SyntaxFactory.ParseCompilationUnit(SyntaxTemplates.FILE_HEADER_SIGNATURE)
                            .AddMembers(
                                SyntaxFactory.NamespaceDeclaration(
                                                 SyntaxFactory.IdentifierName(localisationFile.Namespace))
                                             .WithMembers(
                                                 SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                                     SyntaxFactory.ClassDeclaration(localisationFile.Name)
                                                                  .WithMembers(SyntaxFactory.List(
                                                                      localisationFile.Members
                                                                                      .Select(m => m.Parameters.Length == 0 ? GeneratePropertySyntax(m) : GenerateMethodSyntax(workspace, m))
                                                                                      .Prepend(GeneratePrefixSyntax(localisationFile))
                                                                                      .Append(GenerateGetKeySyntax())))
                                                                  .WithModifiers(
                                                                      new SyntaxTokenList(
                                                                          SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                                          SyntaxFactory.Token(SyntaxKind.StaticKeyword))))));

        /// <summary>
        /// Generates the syntax for a property member.
        /// </summary>
        public static MemberDeclarationSyntax GeneratePropertySyntax(LocalisationMember member)
            => SyntaxFactory.ParseMemberDeclaration(
                string.Format(SyntaxTemplates.PROPERTY_MEMBER_TEMPLATE,
                    member.Name,
                    member.Key,
                    convertToVerbatim(member.EnglishText),
                    member.EnglishText))!;

        /// <summary>
        /// Generates the syntax for a method member.
        /// </summary>
        public static MemberDeclarationSyntax GenerateMethodSyntax(Workspace workspace, LocalisationMember member)
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
                string.Format(SyntaxTemplates.METHOD_MEMBER_TEMPLATE,
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
        public static MemberAccessExpressionSyntax GenerateMemberAccessSyntax(LocalisationFile localisationFile, LocalisationMember member)
            => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(localisationFile.Name),
                SyntaxFactory.IdentifierName(member.Name));

        /// <summary>
        /// Generates the syntax for the prefix constant.
        /// </summary>
        public static MemberDeclarationSyntax GeneratePrefixSyntax(LocalisationFile localisationFile)
            => SyntaxFactory.ParseMemberDeclaration(string.Format(SyntaxTemplates.CONST_PREFIX_TEMPLATE, $"{localisationFile.Namespace}.{localisationFile.Prefix}"))!;

        /// <summary>
        /// Generates the syntax for the getKey() method.
        /// </summary>
        public static MemberDeclarationSyntax GenerateGetKeySyntax()
            => SyntaxFactory.ParseMemberDeclaration(SyntaxTemplates.GET_KEY_METHOD_TEMPLATE)!;

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
