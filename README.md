# ContentSpecId
ContentSpecId is a class written in C# and intended for shared use across multiple applications. A port to Java should be available in the latter part of 2018.

The [Smarter Balanced Assessment Consortium](https://www.smarterbalanced.org) uses Content Specification Identifiers to reference claims and targets in the Smarter Balanced Content Specification. 

This class may be used for the following functions:

*  Generate Content Spec ID strings in Enhanced and Legacy formats from their component properties (Subject, Grade, Claim, and Target).
*  Parse Content Spec ID strings in Enhanced and Legacy formats.
*  Convert Content Spec ID strings between Enhanced and Legacy Formats.

## About Content Specifications

The following resources offer more detail about Smarter Balanced Content Specifications and associated IDs:

* SmarterApp: [Content Specification ID Formats](http://www.smarterapp.org/documents/ContentSpecificationIdFormats.pdf)
* [Smarter Balanced Content Specifications and Competency Frameworks](https://case.smarterbalanced.org/cfdoc/)

## This Project: Shared Code + Unit Test
This project consists of the ContentSpecId class (encapsulated in [ContentSpecId.cs](https://github.com/SmarterApp/SC_ContentSpecId/blob/master/ContentSpecId.cs)) plus unit tests. The tests exercise the class with all identifiers in use or defined. The text files containing IDs were derived as follows:

* **LegacyIdsInUse**: We used a modified version of [TabulateSmarterTestContentPackage](https://github.com/SmarterApp/TabulateSmarterTestContentPackage) to extract all identifiers that were found in the 2017-2018 test content packages. Some of these ids had errors which have been hand-corrected.
* **LegacyIdsWithErrors**: The legacy IDs with errors pre-correction. The unit tests do not make use of this file.
* **ElaLegacyIdsFromCoreStandards**: Extracted from [SBAC-ELA-v1.xlsx](https://github.com/SmarterApp/SS_CoreStandards/blob/master/Documents/Imports/SBAC-ELA-v1.xlsx), the spreadsheet used to load Ela standards the original SmarterApp CoreStandards service.
* **MathLegacyIdsFromCoreStandards**: Extracted from [SBAC-MA-v1.xlsx](https://github.com/SmarterApp/SS_CoreStandards/blob/master/Documents/Imports/SBAC-MA-V1.xlsx), the spreadsheet used to load Math standards into the original SmarterApp CoreStandards service.
* **ElaEnhancedIdsFromCASE**: Extracted from the content specification data layer at [case.smarterbalanced.org](http://case.smarterbalanced.org).
* **MathEnhancedIdsFromCASE**: Extracted from the content specification data layer at [case.smarterbalanced.org](http://case.smarterbalanced.org).

The method, "ExtractIdsFromCASE", included in **Program.cs** can be used to extract identifiers from [CASE](https://www.imsglobal.org/activity/case) format.

## Documentation
Please see the comments in the source code that precede each function for documentation on how to use the class.

## About CodeBits
A [CodeBit](http://FileMeta.org/CodeBit.html) is a simple way to share common source code. Each CodeBit consists of a single source code file. A structured comment at the beginning of the file indicates where to find the master copy so that automated tools can retrieve and update CodeBits to the latest version.

## License
Offered under the [Educational Community License 2.0](https://opensource.org/licenses/ECL-2.0).