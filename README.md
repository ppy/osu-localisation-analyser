# osu! Localisation Analyser

This is a .NET analyser providing code fixes to generate localisations for use in [osu!](https://github.com/ppy/osu).

# `.editorconfig` options

```sh
# Customises the "prefix" namespace value. Defaults to "{AssemblyName}.Localisation".
dotnet_diagnostic.OLOC001.prefix_namespace = Some.Custom.Namespace

# The number of words to use in the source string to generate the target member name. Defaults to all words in the string.
dotnet_diagnostic.OLOC001.words_in_name = 5

# The license header to prepend to the start of the "Strings" classes.
dotnet_diagnostic.OLOC001.license_header = // Line 1 of license header\n// Line 2 of license header
```