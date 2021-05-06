// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.Abstractions.IO;

namespace LocalisationAnalyser.Tests.Helpers.IO
{
    public class MockFileSystem : IFileSystem
    {
        private readonly System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs;

        public MockFileSystem()
        {
            mockFs = new System.IO.Abstractions.TestingHelpers.MockFileSystem();

            Path = new MockPath(mockFs);
            File = new MockFile(mockFs);
            Directory = new MockDirectory(mockFs);
            FileInfo = new MockFileInfoFactory(this, mockFs);
        }

        public IPath Path { get; }
        public IFile File { get; }
        public IDirectory Directory { get; }
        public IFileInfoFactory FileInfo { get; }

        public void AddFile(string path, string contents) => mockFs.AddFile(path, contents);
    }
}
