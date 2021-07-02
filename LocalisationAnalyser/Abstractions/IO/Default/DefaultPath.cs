// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace LocalisationAnalyser.Abstractions.IO.Default
{
    internal class DefaultPath : IPath
    {
        public string GetDirectoryName(string path) => Path.GetDirectoryName(path)!;

        public string Combine(params string[] paths) => Path.Combine(paths);

        public string ChangeExtension(string path, string newExtension) => Path.ChangeExtension(path, newExtension);

        public string GetFileName(string path) => Path.GetFileName(path);

        public string GetFullPath(string path) => Path.GetFullPath(path);
    }
}
