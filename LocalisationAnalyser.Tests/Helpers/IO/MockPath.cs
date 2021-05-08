// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.Abstractions.IO;

namespace LocalisationAnalyser.Tests.Helpers.IO
{
    public class MockPath : IPath
    {
        private readonly System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs;

        public MockPath(System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs)
        {
            this.mockFs = mockFs;
        }

        public string GetDirectoryName(string path) => mockFs.Path.GetDirectoryName(path);

        public string Combine(params string[] paths) => mockFs.Path.Combine(paths);

        public string ChangeExtension(string path, string newExtension) => mockFs.Path.ChangeExtension(path, newExtension);

        public string GetFileName(string path) => mockFs.Path.GetFileName(path);
    }
}
