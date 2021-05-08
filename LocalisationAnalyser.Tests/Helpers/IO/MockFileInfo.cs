// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using LocalisationAnalyser.Abstractions.IO;

namespace LocalisationAnalyser.Tests.Helpers.IO
{
    internal class MockFileInfo : IFileInfo
    {
        private readonly System.IO.Abstractions.IFileInfo fileInfo;

        public MockFileInfo(IFileSystem fileSystem, System.IO.Abstractions.IFileInfo fileInfo)
        {
            FileSystem = fileSystem;
            this.fileInfo = fileInfo;
        }

        public bool Exists => fileInfo.Exists;
        public string FullName => fileInfo.FullName;
        public string DirectoryName => fileInfo.DirectoryName;
        public IFileSystem FileSystem { get; }
        public Stream OpenRead() => fileInfo.OpenRead();
        public Stream OpenWrite() => fileInfo.OpenWrite();
    }
}
