// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace LocalisationAnalyser.Abstractions.IO.Default
{
    internal class DefaultFileInfo : IFileInfo
    {
        public DefaultFileInfo(IFileSystem fileSystem, string fullname)
        {
            FullName = fullname;
            FileSystem = fileSystem;
        }

        public bool Exists => File.Exists(FullName);

        public string FullName { get; }

        public string DirectoryName => Path.GetDirectoryName(FullName)!;

        public IFileSystem FileSystem { get; }

        public Stream OpenRead() => File.OpenRead(FullName);

        public Stream OpenWrite() => File.OpenWrite(FullName);
    }
}
