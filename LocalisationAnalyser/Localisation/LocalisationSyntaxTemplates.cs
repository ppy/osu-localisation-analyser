// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace LocalisationAnalyser.Localisation
{
    public static class LocalisationSyntaxTemplates
    {
        public const string GET_KEY_METHOD_NAME = "getKey";

        /// <summary>
        /// The return type of either property or method members (see: <see cref="PROPERTY_SIGNATURE"/> and <see cref="METHOD_SIGNATURE"/>).
        /// </summary>
        public const string MEMBER_RETURN_TYPE = "LocalisableString";

        /// <summary>
        /// The construction type of either property or method members (see: <see cref="PROPERTY_SIGNATURE"/> and <see cref="METHOD_SIGNATURE"/>).
        /// </summary>
        public const string MEMBER_CONSTRUCTION_TYPE = "TranslatableString";

        public const string PREFIX_CONST_NAME = "prefix";

        /// <summary>
        /// The template signature for the 'prefix' const.
        /// </summary>
        /// <remarks>
        /// {0} : Value
        /// </remarks>
        public static readonly string PREFIX_SIGNATURE = $@"
private const string {PREFIX_CONST_NAME} = @""{{0}}"";
";

        /// <summary>
        /// The template signature for a localisation property.
        /// </summary>
        /// <remarks>
        /// {0} : Name
        /// {1} : Lookup key
        /// {2} : Verbatim english text
        /// {3} : Xmldoc
        /// </remarks>
        public static readonly string PROPERTY_SIGNATURE = $@"
/// <summary>
/// ""{{3}}""
/// </summary>
public static {MEMBER_RETURN_TYPE} {{0}} => new {MEMBER_CONSTRUCTION_TYPE}({GET_KEY_METHOD_NAME}(@""{{1}}""), @""{{2}}"");
";

        /// <summary>
        /// The template signature for a localisation method.
        /// </summary>
        /// <remarks>
        /// {0} : Name
        /// {1} : Method parameters
        /// {2} : Lookup key
        /// {3} : Verbatim english text
        /// {4} : Localisation parameters
        /// {5} : Xmldoc
        /// </remarks>
        public static readonly string METHOD_SIGNATURE = $@"
/// <summary>
/// ""{{5}}""
/// </summary>
public static {MEMBER_RETURN_TYPE} {{0}}{{1}} => new {MEMBER_CONSTRUCTION_TYPE}({GET_KEY_METHOD_NAME}(@""{{2}}""), @""{{3}}"", {{4}});
";

        /// <summary>
        /// The template signature for the 'getKey' method.
        /// </summary>
        // Todo: Ignore the extra newline here - somehow this format messes up R#.
        public static readonly string GET_KEY_SIGNATURE =
            $@"
private static string {GET_KEY_METHOD_NAME}(string key) => $@""{{prefix}}:{{key}}"";";

        public const string LICENSE_HEADER =
            @"// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.";

        public static readonly string FILE_HEADER_SIGNATURE =
            @$"{LICENSE_HEADER}

using osu.Framework.Localisation;";
    }
}
