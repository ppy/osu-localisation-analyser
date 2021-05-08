// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocalisationAnalyser.Localisation
{
    public partial class LocalisationFile
    {
        /// <summary>
        /// A syntax walker to discover all localisation members of a localisation class.
        /// </summary>
        private class Walker : CSharpSyntaxWalker
        {
            public string? Namespace { get; private set; }
            public string? Name { get; private set; }
            public string? Prefix { get; private set; }
            public readonly List<LocalisationMember> Members = new List<LocalisationMember>();

            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                base.VisitNamespaceDeclaration(node);
                Namespace = convertNamespaceToString(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                base.VisitClassDeclaration(node);
                Name = node.Identifier.ValueText;
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                base.VisitVariableDeclarator(node);

                if (node.Identifier.ValueText == LocalisationSyntaxTemplates.PREFIX_CONST_NAME)
                {
                    Prefix = node.ChildNodes()
                                 .OfType<LiteralExpressionSyntax>()
                                 .FirstOrDefault()?
                                 .Token.ValueText;
                }
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                base.VisitPropertyDeclaration(node);
                analyseNode(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                base.VisitMethodDeclaration(node);
                analyseNode(node);
            }

            private static string convertNamespaceToString(NamespaceDeclarationSyntax declaration)
            {
                return getName(declaration.Name);

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

            private void analyseNode(MemberDeclarationSyntax node)
            {
                if (!tryAnalyseMemberDefinition(node, out var name, out var parameters, out var body))
                    return;

                if (!tryAnalyseMemberBody(body, out var key, out var englishText))
                    return;

                Members.Add(new LocalisationMember(name, key, englishText, parameters));
            }

            private bool tryAnalyseMemberDefinition(MemberDeclarationSyntax member, out string name, out LocalisationParameter[] parameters,
                                                    [NotNullWhen(true)] out ArrowExpressionClauseSyntax? body)
            {
                name = string.Empty;
                parameters = Array.Empty<LocalisationParameter>();
                body = null;

                TypeSyntax returnType;

                // Deconstruct member.
                if (member is PropertyDeclarationSyntax propertyMember)
                {
                    returnType = propertyMember.Type;
                    name = propertyMember.Identifier.ValueText;
                    body = propertyMember.ExpressionBody;
                }
                else if (member is MethodDeclarationSyntax methodMember)
                {
                    returnType = methodMember.ReturnType;
                    name = methodMember.Identifier.ValueText;
                    body = methodMember.ExpressionBody;

                    parameters = methodMember.ParameterList.Parameters
                                             .Where(p => p.Type != null)
                                             .Select(p => new LocalisationParameter(p.Type!.ToString(), p.Identifier.ValueText))
                                             .ToArray();
                }
                else
                    return false;

                // Validate return type and member definition.
                if (returnType.ToString() != LocalisationSyntaxTemplates.MEMBER_RETURN_TYPE
                    || body == null)
                {
                    return false;
                }

                return true;
            }

            private bool tryAnalyseMemberBody(ArrowExpressionClauseSyntax body, out string key, out string englishText)
            {
                key = string.Empty;
                englishText = string.Empty;

                // Validate body.
                if (body.Expression is not ObjectCreationExpressionSyntax creationSyntax)
                    return false;

                // Validate creation expression.
                if (creationSyntax.Type.ToString() != LocalisationSyntaxTemplates.MEMBER_CONSTRUCTION_TYPE
                    || creationSyntax.ArgumentList == null
                    || creationSyntax.ArgumentList.Arguments.Count < 2)
                {
                    return false;
                }

                // Validate key argument.
                if (creationSyntax.ArgumentList.Arguments[0].Expression is not InvocationExpressionSyntax getKeyInvocation
                    || getKeyInvocation.Expression.ToString() != LocalisationSyntaxTemplates.GET_KEY_METHOD_NAME
                    || getKeyInvocation.ArgumentList.Arguments.Count == 0
                    || getKeyInvocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax keyLiteral
                    || keyLiteral.Kind() != SyntaxKind.StringLiteralExpression)
                {
                    return false;
                }

                // Validate english literal argument.
                if (creationSyntax.ArgumentList.Arguments[1].Expression is not LiteralExpressionSyntax englishTextLiteral
                    || (englishTextLiteral.Kind() != SyntaxKind.StringLiteralExpression
                        && englishTextLiteral.Kind() != SyntaxKind.InterpolatedStringExpression))
                {
                    return false;
                }

                key = keyLiteral.Token.ValueText;
                englishText = englishTextLiteral.Token.ValueText;

                return true;
            }
        }
    }
}
