# Tangible-Play CodeFormatter Fork

Testing command for Sample Game:
```
git reset --hard HEAD;git submodule foreach --recursive git reset --hard; cf Assembly-CSharp* /folder:Fabric,AssetBundleManager,Plugins,XUPorter
```

[Based on Osmo C# Code Style Here](https://docs.google.com/a/tangibleplay.com/document/d/1rtXuKnotrlpePBOlpOtZmVjAoXZFr8AnECVNLXLwslY/edit?usp=sharing)

## Features
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