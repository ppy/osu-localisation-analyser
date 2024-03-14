# osu! Localisation Analyser

This is a .NET analyser providing code fixes to generate localisations for use in [osu!](https://github.com/ppy/osu).

# `.editorconfig` options

```sh
# The namespace in which the localisation files (.cs) are placed relative to the current project.
# Defaults to `Localisation`.
dotnet_diagnostic.OLOC001.localisation_namespace = Some.Custom.Namespace

# The namespace in which the localisation resources (.resx) are expected to be found.
# The localisation lookup key is formed using this as `{resource_namespace}:{key}`.
# Defaults to `{AssemblyName}.Localisation`.
dotnet_diagnostic.OLOC001.resource_namespace = Some.Custom.Namespace

# The number of words to use in the source string to generate the target member name. Defaults to all words in the string.
dotnet_diagnostic.OLOC001.words_in_name = 5

# The license header to prepend to the start of the "Strings" classes.
dotnet_diagnostic.OLOC001.license_header = // Line 1 of license header\n// Line 2 of license header
```