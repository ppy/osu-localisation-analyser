// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace LocalisationAnalyser.Localisation
{
    public static class LocalisationFileExtensions
    {
        /// <summary>
        /// Writes this <see cref="LocalisationFile"/> to a stream.
        /// </summary>
        /// <param name="file">The <see cref="LocalisationFile"/> to be written to the stream.</param>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="workspace">The workspace to format with.</param>
        /// <param name="options">The analyser options to apply to the document.</param>
        /// <param name="leaveOpen">Whether to leave the given <paramref name="stream"/> open.</param>
        public static async Task WriteAsync(this LocalisationFile file, Stream stream, Workspace workspace, AnalyzerConfigOptions? options = null, bool leaveOpen = false)
        {
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen))
                await sw.WriteAsync(Formatter.Format(SyntaxGenerators.GenerateClassSyntax(workspace, file, options), workspace).ToFullString());
        }
    }
}
