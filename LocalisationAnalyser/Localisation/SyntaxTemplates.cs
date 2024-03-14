// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace LocalisationAnalyser.Localisation
{
    public static class SyntaxTemplates
    {
        /// <summary>
        /// The osu!framework namespace containing localisation-related classes.
        /// </summary>
        public const string FRAMEWORK_LOCALISATION_NAMESPACE = "osu.Framework.Localisation";

        /// <summary>
        /// The return type of either property or method members (see: <see cref="PROPERTY_MEMBER_TEMPLATE"/> and <see cref="METHOD_MEMBER_TEMPLATE"/>).
        /// </summary>
        public const string MEMBER_RETURN_TYPE = "LocalisableString";

        /// <summary>
        /// The construction type of either property or method members (see: <see cref="PROPERTY_MEMBER_TEMPLATE"/> and <see cref="METHOD_MEMBER_TEMPLATE"/>).
        /// </summary>
        public const string MEMBER_CONSTRUCTION_TYPE = "TranslatableString";

        /// <summary>
        /// The construction type of the localisable attribute which replaces <see cref="System.ComponentModel.DescriptionAttribute"/>.
        /// </summary>
        public const string LOCALISABLE_DESCRIPTION_ATTRIBUTE_CONSTRUCTION_TYPE = "LocalisableDescription";

        public const string SETTING_SOURCE_ATTRIBUTE_CONSTRUCTION_TYPE = "SettingSource";

        /// <summary>
        /// The path to localisations relative to the project directory.
        /// </summary>
        public const string DEFAULT_LOCALISATION_NAMESPACE = "Localisation";

        /// <summary>
        /// The name of the 'prefix' const used for building the lookup key.
        /// </summary>
        public const string PREFIX_CONST_NAME = "prefix";

        /// <summary>
        /// The template for the 'prefix' const.
        /// </summary>
        /// <remarks>
        /// {0} : Value
        /// </remarks>
        public static readonly string PREFIX_CONST_TEMPLATE = $@"
private const string {PREFIX_CONST_NAME} = @""{{0}}"";
";

        /// <summary>
        /// The template for a localisation property.
        /// </summary>
        /// <remarks>
        /// {0} : Name
        /// {1} : Lookup key
        /// {2} : Verbatim english text
        /// {3} : Xmldoc
        /// </remarks>
        public static readonly string PROPERTY_MEMBER_TEMPLATE = $@"
/// <summary>
{{3}}
/// </summary>
public static {MEMBER_RETURN_TYPE} {{0}} => new {MEMBER_CONSTRUCTION_TYPE}({GET_KEY_METHOD_NAME}(@""{{1}}""), @""{{2}}"");
";

        /// <summary>
        /// The template for a localisation method.
        /// </summary>
        /// <remarks>
        /// {0} : Name
        /// {1} : Method parameters
        /// {2} : Lookup key
        /// {3} : Verbatim english text
        /// {4} : Localisation parameters
        /// {5} : Xmldoc
        /// </remarks>
        public static readonly string METHOD_MEMBER_TEMPLATE = $@"
/// <summary>
{{5}}
/// </summary>
public static {MEMBER_RETURN_TYPE} {{0}}{{1}} => new {MEMBER_CONSTRUCTION_TYPE}({GET_KEY_METHOD_NAME}(@""{{2}}""), @""{{3}}"", {{4}});
";

        /// <summary>
        /// The name of the 'getKey' method used for localisation lookups.
        /// </summary>
        public const string GET_KEY_METHOD_NAME = "getKey";

        /// <summary>
        /// The template for the 'getKey' method.
        /// </summary>
        public static readonly string GET_KEY_METHOD_TEMPLATE =
            // Ignore the extra newline here - somehow this format messes up R#.
            $@"
private static string {GET_KEY_METHOD_NAME}(string key) => $@""{{prefix}}:{{key}}"";";

        /// <summary>
        /// The suffix attached to a localisation file name.
        /// </summary>
        public const string STRINGS_FILE_SUFFIX = "Strings";

        /// <summary>
        /// The common localisation class name.
        /// </summary>
        public const string COMMON_STRINGS_CLASS_NAME = "Common";

        /// <summary>
        /// The template for the localisation file header.
        /// </summary>
        public static string CreateFileHeader(string localisationNamespace)
        {
            if (localisationNamespace.StartsWith(FRAMEWORK_LOCALISATION_NAMESPACE, StringComparison.Ordinal))
                return "{0}";

            return $"{{0}}using {FRAMEWORK_LOCALISATION_NAMESPACE};";
        }
    }
}
