// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading.Tasks;
using LocalisationAnalyser.Generators;
using Microsoft.CodeAnalysis;
using Xunit;

namespace LocalisationAnalyser.Tests
{
    public class LocalisationClassGeneratorTests
    {
        private const string test_class_name = "TestClass";
        private const string test_file_name = "TestFile";
        private const string test_namespace = "TestNamespace";

        private readonly MockFileSystem mockFs;
        private readonly LocalisationClassGenerator generator;

        public LocalisationClassGeneratorTests()
        {
            mockFs = new MockFileSystem();
            generator = new LocalisationClassGenerator(new AdhocWorkspace(), mockFs.FileInfo.FromFileName(test_file_name), test_namespace, test_class_name);
        }

        [Fact]
        public async Task ClassGeneratedForNoFile()
        {
            await generator.Open();
            await generator.Save();

            checkResult(string.Empty);
        }

        [Fact]
        public async Task EmptyFileContainsNoMembers()
        {
            setupFile($@"namespace {test_namespace}
{{
    class TestClass
    {{
        private const string prefix = ""{test_namespace}.{test_class_name}"";
        private static string getKey(string key) => $""{{prefix}}:{{key}}"";
    }}
}}");

            await generator.Open();

            Assert.Empty(generator.Members);
        }

        [Fact]
        public async Task PropertyIsGeneratedFromNoParameters()
        {
            await generator.Open();
            var memberAccess = generator.AddMember(new LocalisationMember("TestProperty", "TestKey", "TestEnglish"));
            await generator.Save();

            checkResult($@"
        /// <summary>
        /// ""TestEnglish""
        /// </summary>
        public static LocalisableString TestProperty => new TranslatableString(getKey(""TestKey""), ""TestEnglish"");
");
        }

        private void setupFile(string contents)
        {
            mockFs.AddFile(test_file_name, contents);
        }

        private void checkResult(string inner)
        {
            var sb = new StringBuilder();

            sb.Append($@"namespace {test_namespace}
{{
    class {test_class_name}
    {{
        private const string prefix = ""{test_namespace}.{test_class_name}"";");

            if (!string.IsNullOrEmpty(inner))
                sb.Append(inner);

            sb.Append(@"        private static string getKey(string key) => $""{prefix}:{key}"";
    }
}");

            Assert.Equal(sb.ToString().Trim(), mockFs.File.ReadAllText(test_file_name));
        }
    }
}
