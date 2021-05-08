// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace LocalisationAnalyser.Generators
{
    public partial class LocalisationClass
    {
        public readonly ImmutableArray<LocalisationMember> Members;
        public readonly string Namespace;
        public readonly string Name;
        public readonly string Prefix;

        public LocalisationClass(string @namespace, string name, string prefix, params LocalisationMember[] members)
        {
            Namespace = @namespace;
            Name = name;
            Prefix = prefix;
            Members = members.ToImmutableArray();
        }

        public LocalisationClass WithMembers(params LocalisationMember[] members)
            => new LocalisationClass(Namespace, Name, Prefix, members);

        public async Task WriteAsync(Stream stream, Workspace workspace)
        {
            using (var sw = new StreamWriter(stream))
                await sw.WriteAsync(Formatter.Format(SyntaxHelpers.GenerateClassSyntax(workspace, this), workspace).ToFullString());
        }

        /// <summary>
        /// Reads a <see cref="LocalisationClass"/> from a file.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The <see cref="LocalisationClass"/>.</returns>
        /// <exception cref="LocalisationClassMalformedException">If the file doesn't contain a valid <see cref="LocalisationClass"/>.</exception>
        public static async Task<LocalisationClass> ReadAsync(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(await sr.ReadToEndAsync());
                var syntaxRoot = await syntaxTree.GetRootAsync();

                var walker = new Walker();
                walker.Visit(syntaxRoot);

                if (string.IsNullOrEmpty(walker.Namespace))
                    throw new LocalisationClassMalformedException("The localisation file contains no namespace.");

                if (string.IsNullOrEmpty(walker.Name))
                    throw new LocalisationClassMalformedException("The localisation file contains no class.");

                if (string.IsNullOrEmpty(walker.Prefix))
                    throw new LocalisationClassMalformedException("The localisation file contains no prefix identifier");

                return new LocalisationClass(walker.Namespace, walker.Name, walker.Prefix, walker.Members.ToArray());
            }
        }
    }
}
