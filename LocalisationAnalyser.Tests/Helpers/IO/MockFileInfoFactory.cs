// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.Abstractions.IO;

namespace LocalisationAnalyser.Tests.Helpers.IO
{
    public class MockFileInfoFactory : IFileInfoFactory
    {
        private readonly IFileSystem fileSystem;
        private readonly System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs;

        public MockFileInfoFactory(IFileSystem fileSystem, System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs)
        {
            this.fileSystem = fileSystem;
            this.mockFs = mockFs;
        }

        public IFileInfo FromFileName(string filename) => new MockFileInfo(fileSystem, mockFs.FileInfo.FromFileName(filename));
    }
}
