// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.Abstractions.IO;

namespace LocalisationAnalyser.Tests.Helpers.IO
{
    internal class MockFile : IFile
    {
        private readonly System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs;

        public MockFile(System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs)
        {
            this.mockFs = mockFs;
        }

        public string ReadAllText(string fullname) => mockFs.File.ReadAllText(fullname);
    }
}
