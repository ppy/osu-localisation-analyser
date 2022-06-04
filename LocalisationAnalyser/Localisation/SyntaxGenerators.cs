// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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
        public static SyntaxNode GenerateClassSyntax(Workspace workspace, LocalisationFile localisationFile, AnalyzerConfigOptions? options)
            => GenerateFileHeaderSyntax(options)
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

        public static CompilationUnitSyntax GenerateFileHeaderSyntax(AnalyzerConfigOptions? options)
        {
            if (options == null || !options.TryGetValue($"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.license_header", out string licenseHeader))
                licenseHeader = string.Empty;

            string[] lines = licenseHeader.Split(new[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);

            var builder = new StringBuilder();

            foreach (var line in lines)
            {
                if (!line.StartsWith("//"))
                    builder.Append("// ");
                builder.AppendLine(line);
            }

            // One extra line at the end (before the using declarations).
            if (lines.Length > 0)
                builder.AppendLine();

            return SyntaxFactory.ParseCompilationUnit(
                string.Format(SyntaxTemplates.FILE_HEADER_TEMPLATE,
                    builder));
        }

        /// <summary>
        /// Generates the syntax for a property member.
        /// </summary>
        public static MemberDeclarationSyntax GeneratePropertySyntax(LocalisationMember member)
            => SyntaxFactory.ParseMemberDeclaration(
                string.Format(SyntaxTemplates.PROPERTY_MEMBER_TEMPLATE,
                    member.Name,
                    member.Key,
                    convertToVerbatim(member.EnglishText),
                    EncodeXmlDoc(member.XmlDoc)))!;

        /// <summary>
        /// Generates the syntax for a method member.
        /// </summary>
        public static MemberDeclarationSyntax GenerateMethodSyntax(Workspace workspace, LocalisationMember member)
        {
            var paramList = SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    member.Parameters.Select(param => SyntaxFactory.Parameter(
                                                                       GenerateIdentifier(param.Name))
                                                                   .WithType(
                                                                       SyntaxFactory.IdentifierName(param.Type)))));

            var argList = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    member.Parameters.Select(param => SyntaxFactory.Argument(
                        GenerateIdentifierName(param.Name)))));

            return SyntaxFactory.ParseMemberDeclaration(
                string.Format(SyntaxTemplates.METHOD_MEMBER_TEMPLATE,
                    member.Name,
                    Formatter.Format(paramList, workspace).ToFullString(),
                    member.Key,
                    convertToVerbatim(member.EnglishText),
                    trimParens(Formatter.Format(argList, workspace).ToFullString()), // The entire string minus the parens
                    EncodeXmlDoc(member.XmlDoc)))!;

            static string trimParens(string input) => input.Substring(1, input.Length - 2);
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
            => SyntaxFactory.ParseMemberDeclaration(string.Format(SyntaxTemplates.PREFIX_CONST_TEMPLATE, localisationFile.Prefix))!;

        /// <summary>
        /// Generates the syntax for the getKey() method.
        /// </summary>
        public static MemberDeclarationSyntax GenerateGetKeySyntax()
            => SyntaxFactory.ParseMemberDeclaration(SyntaxTemplates.GET_KEY_METHOD_TEMPLATE)!;

        /// <summary>
        /// Generates an identifier <see cref="SyntaxToken"/> from a string, taking into account reserved language keywords.
        /// </summary>
        /// <param name="name">The string to generate an identifier for.</param>
        public static SyntaxToken GenerateIdentifier(string name)
        {
            if (SyntaxFacts.IsReservedKeyword(SyntaxFacts.GetKeywordKind(name)))
                name = $"@{name}";
            return SyntaxFactory.Identifier(name);
        }

        /// <summary>
        /// Generates an <see cref="IdentifierNameSyntax"/> from a string, taking into account reserved language keywords.
        /// </summary>
        /// <param name="name">The string to generate an identifier for.</param>
        public static IdentifierNameSyntax GenerateIdentifierName(string name)
        {
            if (SyntaxFacts.IsReservedKeyword(SyntaxFacts.GetKeywordKind(name)))
                name = $"@{name}";
            return SyntaxFactory.IdentifierName(name);
        }

        /// <summary>
        /// Generates syntax to access a <see cref="MemberAccessExpressionSyntax"/> with optional parameters.
        /// </summary>
        /// <param name="memberAccess">The member to access.</param>
        /// <param name="parameterValues">Any parameters to perform the access with.</param>
        /// <returns>The parameterised expression syntax.</returns>
        public static ExpressionSyntax GenerateDirectAccessSyntax(MemberAccessExpressionSyntax memberAccess, IEnumerable<ExpressionSyntax> parameterValues)
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

        /// <summary>
        /// Generates syntax to access a <see cref="MemberAccessExpressionSyntax"/> via an attribute.
        /// </summary>
        /// <param name="memberAccess">The member to access.</param>
        /// <returns>The <see cref="AttributeSyntax"/>.</returns>
        public static AttributeSyntax GenerateAttributeAccessSyntax(MemberAccessExpressionSyntax memberAccess)
            => SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName(SyntaxTemplates.ATTRIBUTE_CONSTRUCTION_TYPE))
                            .WithArgumentList(
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.AttributeArgument(
                                                SyntaxFactory.TypeOfExpression(((IdentifierNameSyntax)memberAccess.Expression))),
                                            SyntaxFactory.AttributeArgument(
                                                SyntaxFactory.ParseExpression($"nameof({memberAccess.Expression}.{memberAccess.Name})"))
                                        }
                                    )));

        /// <summary>
        /// Checks for and adds a new using directive to the given <see cref="CompilationUnitSyntax"/> if required.
        /// </summary>
        /// <param name="node">The <see cref="CompilationUnitSyntax"/> to add the using directive to.</param>
        /// <param name="directive">The directive to add.</param>
        /// <returns>The new <see cref="CompilationUnitSyntax"/>.</returns>
        public static CompilationUnitSyntax AddUsingDirectiveIfNotExisting(CompilationUnitSyntax node, string directive)
        {
            if (node.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(convertUsingDirectiveToString).Any(ud => ud == directive))
                return node;

            return node.AddUsings(SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName(directive)));
        }

        public static string EncodeXmlDoc(string text)
        {
            var lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var sb = new StringBuilder();

                sb.Append("/// ");

                if (i == 0)
                    sb.Append("\"");

                sb.Append(HttpUtility.HtmlEncode(lines[i]));

                if (i == lines.Length - 1)
                    sb.Append("\"");

                lines[i] = sb.ToString();
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static string DecodeXmlDoc(string xmlDoc)
        {
            xmlDoc = xmlDoc.Trim();

            // A valid XMLDoc is formulated as: "text". We need to remove only one set of quotes from either side, so Trim() is too greedy.
            if (xmlDoc.Length > 0)
                xmlDoc = xmlDoc.Substring(1);
            if (xmlDoc.Length > 0)
                xmlDoc = xmlDoc.Substring(0, xmlDoc.Length - 1);

            return HttpUtility.HtmlDecode(xmlDoc);
        }

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

        /// <summary>
        /// Converts a using directive to a fully qualified string.
        /// </summary>
        /// <param name="directive">The top-level using directive.</param>
        /// <returns>The fully qualified string consisting of all namespaces in the using directive.</returns>
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
