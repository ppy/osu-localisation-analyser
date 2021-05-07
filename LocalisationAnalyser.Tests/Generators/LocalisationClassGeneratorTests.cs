// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LocalisationAnalyser.Generators;
using LocalisationAnalyser.Tests.Helpers.IO;
using Microsoft.CodeAnalysis;
using Xunit;

namespace LocalisationAnalyser.Tests.Generators
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
            generator = new LocalisationClassGenerator(new AdhocWorkspace(), mockFs.FileInfo.FromFileName(test_file_name), test_namespace, test_class_name, test_class_name);
        }

        [Fact]
        public async Task ClassGeneratedForNoFile()
        {
            await generator.Open();
            await generator.Save();

            await checkResult(string.Empty);
        }

        [Fact]
        public async Task EmptyFileContainsNoMembers()
        {
            setupFile($@"// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace {test_namespace}
{{
    public static class TestClass
    {{
        private const string prefix = @""{test_namespace}.{test_class_name}"";

        private static string getKey(string key) => $@""{{prefix}}:{{key}}"";
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

            await checkResult($@"
        /// <summary>
        /// ""{english_text}""
        /// </summary>
        public static LocalisableString {prop_name} => new TranslatableString(getKey(@""{key_name}""), @""{english_text}"");
");

            Assert.Equal(test_class_name, memberAccess.Expression.ToString());
            Assert.Equal(prop_name, memberAccess.Name.ToString());
        }

        [Fact]
        public async Task CheckPropertyMemberIsReadCorrectly()
        {
            const string prop_name = "TestProperty";
            const string key_name = "TestKey";
            const string english_text = "TestEnglish";

            setupFile($@"// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

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

            await generator.Open();

            Assert.Single(generator.Members);
            Assert.Equal(prop_name, generator.Members[0].Name);
            Assert.Equal(key_name, generator.Members[0].Key);
            Assert.Equal(english_text, generator.Members[0].EnglishText);
            Assert.Empty(generator.Members[0].Parameters);
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

            await checkResult($@"
        /// <summary>
        /// ""{english_text}""
        /// </summary>
        public static LocalisableString {method_name}({param1.Type} {param1.Name}, {param2.Type} {param2.Name}, {param3.Type} {param3.Name}) => new TranslatableString(getKey(@""{key_name}""), @""{english_text}"", {param1.Name}, {param2.Name}, {param3.Name});
");

            Assert.Equal(test_class_name, memberAccess.Expression.ToString());
            Assert.Equal(method_name, memberAccess.Name.ToString());
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

            setupFile($@"// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

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

            await generator.Open();

            Assert.Single(generator.Members);
            Assert.Equal(method_name, generator.Members[0].Name);
            Assert.Equal(key_name, generator.Members[0].Key);
            Assert.Equal(english_text, generator.Members[0].EnglishText);

            Assert.Equal(3, generator.Members[0].Parameters.Length);
            Assert.Equal(param1, generator.Members[0].Parameters[0]);
            Assert.Equal(param2, generator.Members[0].Parameters[1]);
            Assert.Equal(param3, generator.Members[0].Parameters[2]);
        }

        [Fact]
        public async Task CheckVerbatimStringIsConvertedToLiteral()
        {
            const string prop_name = "TestProperty";
            const string key_name = "TestKey";

            setupFile($@"// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

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

            await generator.Open();

            Assert.Single(generator.Members);
            Assert.Equal(prop_name, generator.Members[0].Name);
            Assert.Equal(key_name, generator.Members[0].Key);
            Assert.Equal("this is a \"verbatim\" string", generator.Members[0].EnglishText);
            Assert.Empty(generator.Members[0].Parameters);
        }

        private void setupFile(string contents)
        {
            mockFs.AddFile(test_file_name, contents);
        }

        private async Task checkResult(string inner)
        {
            var sb = new StringBuilder();

            sb.Append($@"// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

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
