# Tangible-Play CodeFormatter Fork

Testing command for Sample Game:
```
git reset --hard HEAD;git submodule foreach --recursive git reset --hard; cf Assembly-CSharp* /folder:Fabric,AssetBundleManager,Plugins,XUPorter
```

# How to Install
(instructions tested with OSX 10.12)

Mac install instructions originally based on [PR #245](https://github.com/dotnet/codeformatter/pull/245) on [dotnet/codeformatter](https://github.com/dotnet/codeformatter).

1. Requires [Mono](http://www.mono-project.com/). It doesn't compile with latest Mono  version (4.8), use [4.6.2](https://download.mono-project.com/archive/4.6.2/) instead.

2. Run `sh init-tools.sh` (downloads and unzips MSBuild for Mono).

3. Restore NuGet packages:
`nuget restore src/CodeFormatter.sln`

4. Build the project (the script copies the built CodeFormatter project to /usr/local/bin/CodeFormatter):
`sh rebuild-codeformatter.sh`

5. Run the codeformatter on your Unity project:
`mono /usr/local/bin/CodeFormatter/CodeFormatter.exe Assembly-CSharp-*` in your project directory.

If `Assembly-CSharp` solution files are not found in your project directory, then you need to open the Unity project and click on `Assets -> Open C# Project` to generate the solution files.


## Features
[Based on Osmo C# Code Style Here](https://docs.google.com/a/tangibleplay.com/document/d/1rtXuKnotrlpePBOlpOtZmVjAoXZFr8AnECVNLXLwslY/edit?usp=sharing)

#### Same as dotnet/codeformatter
* Removes extraneous lines
* Removes usages of `this` if not needed.

#### New in tangibleplay/codeformatter
* Formats files to use tabs (instead of spaces).
* Formats files to put brackets on same line (K&R, instead of on new lines).
* Removes usages of identifiers if not needed.
	* Ex: `MyClass.MyProperty` becomes `MyProperty` if used inside `MyClass`.
* Renames non-public fields to `nonPublicField_` and constant fields like `kConstantField`.
* Renames non-public properties to `NonPublicProperty_`.
* Renames local variables to camel-case `localVariable`.
* Capitalizes non-public methods if not capitalized.
* Add ability to ignore files that are in folders by passing arguments to command line tool like `/folders:Plugins` will ignore files that are in a folder named `Plugins`.
* Disabled escaping literals.
* Various bug-fixes.

## Known Issues
* Removing usages of `this` and identifiers formats the text with spaces and CLRF (\n\r - windows newline).
	* Current work-around: run formatter twice..