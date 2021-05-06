// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using LocalisationAnalyser.Abstractions.IO;

namespace LocalisationAnalyser.Tests.Helpers.IO
{
    public class MockFile : IFile
    {
        private readonly System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs;

        public MockFile(System.IO.Abstractions.TestingHelpers.MockFileSystem mockFs)
        {
            this.mockFs = mockFs;
        }

        public Task<string> ReadAllTextAsync(string fullname, CancellationToken cancellationToken) => mockFs.File.ReadAllTextAsync(fullname, cancellationToken);
    }
}
