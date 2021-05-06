using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
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

        private readonly Workspace workspace;
        private readonly string className;
        private readonly IFileInfo classFile;
        private readonly string targetNamespace;

        private ClassDeclarationSyntax? classSyntax;

        /// <summary>
        /// Creates a new localisation class generator.
        /// </summary>
        /// <param name="workspace">The generation workspace, used for code formatting.</param>
        /// <param name="classFile">The localisation class file.</param>
        /// <param name="classNamespace">The localisation class namespace.</param>
        /// <param name="className">The localisation class name.</param>
        public LocalisationClassGenerator(Workspace workspace, IFileInfo classFile, string classNamespace, string className)
        {
            this.workspace = workspace;
            this.className = className;
            this.classFile = classFile;
            this.targetNamespace = classNamespace;
        }

        /// <summary>
        /// Opens the localisation class, or creates a new one if one doesn't already exist.
        /// </summary>
        public async Task Open()
        {
            if (classFile.Exists)
            {
                using (var sr = new StreamReader(classFile.OpenRead()))
                {
                    var syntaxTree = CSharpSyntaxTree.ParseText(await sr.ReadToEndAsync());
                    var syntaxRoot = await syntaxTree.GetRootAsync();
                    classSyntax = syntaxRoot.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().SingleOrDefault(c => c.Identifier.ToString() == className);
                }
            }

            classSyntax ??= SyntaxFactory.ClassDeclaration(className)
                                         .WithMembers(SyntaxFactory.List(new[]
                                         {
                                             generatePrefixSyntax(),
                                             generateGetKeySyntax()
                                         }));

            var walker = new LocalisationClassWalker();
            classSyntax.Accept(walker);
            members.AddRange(walker.Members);
        }

        /// <summary>
        /// Generates and saves the localisation class.
        /// </summary>
        public async Task Save()
        {
            if (classSyntax == null)
                throw new InvalidOperationException("Class not opened.");

            using (var sw = new StreamWriter(classFile.OpenWrite()))
                await sw.WriteAsync(Formatter.Format(generateClassSyntax(), workspace).ToFullString());
        }

        /// <summary>
        /// Adds a new member to the localisation class.
        /// </summary>
        /// <param name="member">The member to add.</param>
        /// <returns>A <see cref="MemberAccessExpressionSyntax"/> that can be used to refer to the added member.</returns>
        public MemberAccessExpressionSyntax AddMember(LocalisationMember member)
        {
            if (classSyntax == null)
                throw new InvalidOperationException("Class not opened.");

            members.Add(member);
            return generateMemberAccessSyntax(member);
        }

        /// <summary>
        /// Removes a member from the localisation class.
        /// </summary>
        /// <param name="member">The member to remove.</param>
        public void RemoveMember(LocalisationMember member)
        {
            if (classSyntax == null)
                throw new InvalidOperationException("Class not opened.");

            members.Remove(member);
        }

        /// <summary>
        /// Generates the full class syntax, including the namespace, all leading/trailing members, and the localisation members.
        /// </summary>
        /// <returns>The syntax.</returns>
        private SyntaxNode generateClassSyntax()
            => SyntaxFactory.NamespaceDeclaration(
                                SyntaxFactory.IdentifierName(targetNamespace))
                            .WithMembers(
                                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                    classSyntax!.WithMembers(
                                        SyntaxFactory.List(
                                            Members.Select(m => m.Parameters.Length == 0 ? generatePropertySyntax(m) : generateMethodSyntax(m))
                                                   .Prepend(generatePrefixSyntax())
                                                   .Append(generateGetKeySyntax())))));

        /// <summary>
        /// Generates the syntax for a property member.
        /// </summary>
        private MemberDeclarationSyntax generatePropertySyntax(LocalisationMember member)
            => SyntaxFactory.ParseMemberDeclaration(
                string.Format(LocalisationClassTemplates.PROPERTY_SIGNATURE,
                    member.Name,
                    member.Key,
                    member.EnglishText,
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
                    member.EnglishText,
                    Formatter.Format(argList, workspace).ToFullString()[1..^1], // The entire string minus the parens
                    member.EnglishText))!; // Todo: Improve xmldoc
        }

        /// <summary>
        /// Generates the syntax for accessing the member.
        /// </summary>
        private MemberAccessExpressionSyntax generateMemberAccessSyntax(LocalisationMember member)
            => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(className),
                SyntaxFactory.IdentifierName(member.Name));

        /// <summary>
        /// Generates the syntax for the prefix constant.
        /// </summary>
        private MemberDeclarationSyntax generatePrefixSyntax()
            => SyntaxFactory.ParseMemberDeclaration(string.Format(LocalisationClassTemplates.PREFIX_SIGNATURE, $"{targetNamespace}.{className}"))!;

        /// <summary>
        /// Generates the syntax for the getKey() method.
        /// </summary>
        private MemberDeclarationSyntax generateGetKeySyntax()
            => SyntaxFactory.ParseMemberDeclaration(LocalisationClassTemplates.GET_KEY_SIGNATURE)!;
    }
}
