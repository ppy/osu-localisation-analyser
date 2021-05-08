// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LocalisationAnalyser.Localisation;
using LocalisationAnalyser.Tests.Helpers.IO;
using Microsoft.CodeAnalysis;
using Xunit;

namespace LocalisationAnalyser.Tests.Localisation
{
    public class LocalisationFileTests
    {
        private const string test_class_name = "TestClass";
        private const string test_file_name = "TestFile";
        private const string test_namespace = "TestNamespace";

        private readonly MockFileSystem mockFs;
        private readonly Workspace workspace;

        public LocalisationFileTests()
        {
            mockFs = new MockFileSystem();
            workspace = new AdhocWorkspace();
        }

        [Fact]
        public async Task ClassGeneratedForNoFile()
        {
            await setupLocalisation();
            await checkResult(string.Empty);
        }

        [Fact]
        public async Task EmptyFileContainsNoMembers()
        {
            var localisation = await setupFile($@"{LocalisationSyntaxTemplates.FILE_HEADER_SIGNATURE}

namespace {test_namespace}
{{
    public static class TestClass
    {{
        private const string prefix = @""{test_namespace}.{test_class_name}"";

        private static string getKey(string key) => $@""{{prefix}}:{{key}}"";
    }}
}}");

            Assert.Empty(localisation.Members);
        }

        [Fact]
        public async Task PropertyIsGeneratedFromNoParameters()
        {
            const string prop_name = "TestProperty";
            const string key_name = "TestKey";
            const string english_text = "TestEnglish";

            await setupLocalisation(new LocalisationMember(prop_name, key_name, english_text));

            await checkResult($@"
        /// <summary>
        /// ""{english_text}""
        /// </summary>
        public static LocalisableString {prop_name} => new TranslatableString(getKey(@""{key_name}""), @""{english_text}"");
");
        }

        [Fact]
        public async Task CheckPropertyMemberIsReadCorrectly()
        {
            const string prop_name = "TestProperty";
            const string key_name = "TestKey";
            const string english_text = "TestEnglish";

            var localisation = await setupFile($@"{LocalisationSyntaxTemplates.FILE_HEADER_SIGNATURE}

namespace {test_namespace}
{{
    public static class TestClass
    {{
        private const string prefix = @""{test_namespace}.{test_class_name}"";

        /// <summary>
        /// ""{english_text}""
        /// </summary>
        public static LocalisableString {prop_name} => new TranslatableString(getKey(@""{key_name}""), @""{english_text}"");

        private static string getKey(string key) => $@""{{prefix}}:{{key}}"";
    }}
}}");

            Assert.Single(localisation.Members);
            Assert.Equal(prop_name, localisation.Members[0].Name);
            Assert.Equal(key_name, localisation.Members[0].Key);
            Assert.Equal(english_text, localisation.Members[0].EnglishText);
            Assert.Empty(localisation.Members[0].Parameters);
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

            await setupLocalisation(new LocalisationMember(method_name, key_name, english_text, param1, param2, param3));

            await checkResult($@"
        /// <summary>
        /// ""{english_text}""
        /// </summary>
        public static LocalisableString {method_name}({param1.Type} {param1.Name}, {param2.Type} {param2.Name}, {param3.Type} {param3.Name}) => new TranslatableString(getKey(@""{key_name}""), @""{english_text}"", {param1.Name}, {param2.Name}, {param3.Name});
");
        }

        [Fact]
        public async Task CheckMethodMemberIsReadCorrectly()
        {
            const string method_name = "TestMethod";
            const string key_name = "TestKey";
            const string english_text = "TestEnglish{0}{1}{2}";

            var param1 = new LocalisationParameter("int", "first");
            var param2 = new LocalisationParameter("string", "second");
            var param3 = new LocalisationParameter("customobj", "third");

            var localisation = await setupFile($@"{LocalisationSyntaxTemplates.FILE_HEADER_SIGNATURE}

namespace {test_namespace}
{{
    public static class TestClass
    {{
        private const string prefix = ""{test_namespace}.{test_class_name}"";

        /// <summary>
        /// ""{english_text}""
        /// </summary>
        public static LocalisableString {method_name}({param1.Type} {param1.Name}, {param2.Type} {param2.Name}, {param3.Type} {param3.Name}) => new TranslatableString(getKey(""{key_name}""), ""{english_text}"", {param1.Name}, {param2.Name}, {param3.Name});

        private static string getKey(string key) => $""{{prefix}}:{{key}}"";
    }}
}}");

            Assert.Single(localisation.Members);
            Assert.Equal(method_name, localisation.Members[0].Name);
            Assert.Equal(key_name, localisation.Members[0].Key);
            Assert.Equal(english_text, localisation.Members[0].EnglishText);

            Assert.Equal(3, localisation.Members[0].Parameters.Length);
            Assert.Equal(param1, localisation.Members[0].Parameters[0]);
            Assert.Equal(param2, localisation.Members[0].Parameters[1]);
            Assert.Equal(param3, localisation.Members[0].Parameters[2]);
        }

        [Fact]
        public async Task CheckVerbatimStringIsConvertedToLiteral()
        {
            const string prop_name = "TestProperty";
            const string key_name = "TestKey";

            var localisation = await setupFile($@"{LocalisationSyntaxTemplates.FILE_HEADER_SIGNATURE}

namespace {test_namespace}
{{
    public static class TestClass
    {{
        private const string prefix = @""{test_namespace}.{test_class_name}"";

        /// <summary>
        /// ""this is a ""verbatim"" string""
        /// </summary>
        public static LocalisableString {prop_name} => new TranslatableString(getKey(@""{key_name}""), @""this is a """"verbatim"""" string"");

        private static string getKey(string key) => $@""{{prefix}}:{{key}}"";
    }}
}}");

            Assert.Single(localisation.Members);
            Assert.Equal(prop_name, localisation.Members[0].Name);
            Assert.Equal(key_name, localisation.Members[0].Key);
            Assert.Equal("this is a \"verbatim\" string", localisation.Members[0].EnglishText);
            Assert.Empty(localisation.Members[0].Parameters);
        }

        private async Task<LocalisationFile> setupFile(string contents)
        {
            mockFs.AddFile(test_file_name, contents);
            using (var stream = mockFs.FileInfo.FromFileName(test_file_name).OpenRead())
                return await LocalisationFile.ReadAsync(stream);
        }

        private async Task setupLocalisation(params LocalisationMember[] members)
        {
            using (var stream = mockFs.FileInfo.FromFileName(test_file_name).OpenWrite())
                await new LocalisationFile(test_namespace, test_class_name, test_class_name, members).WriteAsync(stream, workspace);
        }

        private async Task checkResult(string inner)
        {
            var sb = new StringBuilder();

            sb.Append($@"{LocalisationSyntaxTemplates.FILE_HEADER_SIGNATURE}

namespace {test_namespace}
{{
    public static class {test_class_name}
    {{
        private const string prefix = @""{test_namespace}.{test_class_name}"";
");

            if (!string.IsNullOrEmpty(inner))
                sb.Append(inner);
            sb.AppendLine();

            sb.Append(@"        private static string getKey(string key) => $@""{prefix}:{key}"";
    }
}");

            Assert.Equal(sb.ToString().Trim(), await mockFs.File.ReadAllTextAsync(test_file_name, CancellationToken.None));
        }
    }
}
