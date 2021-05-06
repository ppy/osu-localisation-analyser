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
            const string prop_name = "TestProperty";
            const string key_name = "TestKey";
            const string english_text = "TestEnglish";

            await generator.Open();
            var memberAccess = generator.AddMember(new LocalisationMember(prop_name, key_name, english_text));
            await generator.Save();

            checkResult($@"
        /// <summary>
        /// ""{english_text}""
        /// </summary>
        public static LocalisableString {prop_name} => new TranslatableString(getKey(""{key_name}""), ""{english_text}"");
");

            Assert.Equal(test_class_name, memberAccess.Expression.ToString());
            Assert.Equal(prop_name, memberAccess.Name.ToString());
        }

        [Fact]
        public async Task MethodIsGeneratedFromParameters()
        {
            const string method_name = "TestMethod";
            const string key_name = "TestKey";
            const string english_text = "TestEnglish{0}{1}{2}";

            var param1 = new LocalisationParameter("int", "first");
            var param2 = new LocalisationParameter("string", "second");
            var param3 = new LocalisationParameter("customobj", "third");

            await generator.Open();
            var memberAccess = generator.AddMember(new LocalisationMember(method_name, key_name, english_text, new[] { param1, param2, param3 }));
            await generator.Save();

            checkResult($@"
        /// <summary>
        /// ""{english_text}""
        /// </summary>
        public static LocalisableString {method_name}({param1.Type} {param1.Name}, {param2.Type} {param2.Name}, {param3.Type} {param3.Name}) => new TranslatableString(getKey(""{key_name}""), ""{english_text}"", {param1.Name}, {param2.Name}, {param3.Name});
");

            Assert.Equal(test_class_name, memberAccess.Expression.ToString());
            Assert.Equal(method_name, memberAccess.Name.ToString());
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
            else
                sb.AppendLine();

            sb.Append(@"        private static string getKey(string key) => $""{prefix}:{key}"";
    }
}");

            Assert.Equal(sb.ToString().Trim(), mockFs.File.ReadAllText(test_file_name));
        }
    }
}
