// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace LocalisationAnalyser.Abstractions.IO.Default
{
    public class DefaultFileInfoFactory : IFileInfoFactory
    {
        private readonly IFileSystem fileSystem;

        public DefaultFileInfoFactory(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public IFileInfo FromFileName(string filename) => new DefaultFileInfo(fileSystem, filename);
    }
}
