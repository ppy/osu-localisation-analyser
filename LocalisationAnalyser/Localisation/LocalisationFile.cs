// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace LocalisationAnalyser.Localisation
{
    /// <summary>
    /// A localisation file.
    /// </summary>
    public partial class LocalisationFile : IEquatable<LocalisationFile>
    {
        /// <summary>
        /// The namespace of the localisation class in this file.
        /// </summary>
        public readonly string Namespace;

        /// <summary>
        /// The name of the localisation class stored in this file.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The localisation prefix of the localisation class in this file.
        /// </summary>
        public readonly string Prefix;

        /// <summary>
        /// All localisation members (properties and methodS) of the localisation class in this file.
        /// </summary>
        public readonly ImmutableArray<LocalisationMember> Members;

        public LocalisationFile(string @namespace, string name, string prefix, params LocalisationMember[] members)
        {
            Namespace = @namespace;
            Name = name;
            Prefix = prefix;
            Members = members.ToImmutableArray();
        }

        /// <summary>
        /// Creates a new <see cref="LocalisationFile"/> with a new set of members.
        /// </summary>
        /// <param name="members">The new localisation members.</param>
        /// <returns>The resultant <see cref="LocalisationFile"/>.</returns>
        public LocalisationFile WithMembers(params LocalisationMember[] members)
            => new LocalisationFile(Namespace, Name, Prefix, members);

        /// <summary>
        /// Writes this <see cref="LocalisationFile"/> to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="workspace">The workspace to format with.</param>
        public async Task WriteAsync(Stream stream, Workspace workspace)
        {
            using (var sw = new StreamWriter(stream))
                await sw.WriteAsync(Formatter.Format(SyntaxGenerators.GenerateClassSyntax(workspace, this), workspace).ToFullString());
        }

        /// <summary>
        /// Reads a <see cref="LocalisationFile"/> from a file.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The <see cref="LocalisationFile"/>.</returns>
        /// <exception cref="MalformedLocalisationException">If the file doesn't contain a valid <see cref="LocalisationFile"/>.</exception>
        public static async Task<LocalisationFile> ReadAsync(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(await sr.ReadToEndAsync());
                var syntaxRoot = await syntaxTree.GetRootAsync();

                var walker = new Walker();
                walker.Visit(syntaxRoot);

                if (string.IsNullOrEmpty(walker.Namespace))
                    throw new MalformedLocalisationException("The localisation file contains no namespace.");

                if (string.IsNullOrEmpty(walker.Name))
                    throw new MalformedLocalisationException("The localisation file contains no class.");

                if (string.IsNullOrEmpty(walker.Prefix))
                    throw new MalformedLocalisationException("The localisation file contains no prefix identifier");

                return new LocalisationFile(walker.Namespace!, walker.Name!, walker.Prefix!.Replace($"{walker.Namespace}.", string.Empty), walker.Members.ToArray());
            }
        }

        public bool Equals(LocalisationFile? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Namespace == other.Namespace && Name == other.Name && Prefix == other.Prefix && Members.Equals(other.Members);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((LocalisationFile)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Namespace.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Prefix.GetHashCode();
                hashCode = (hashCode * 397) ^ Members.GetHashCode();
                return hashCode;
            }
        }
    }
}
