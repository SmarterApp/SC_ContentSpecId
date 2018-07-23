using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using SmarterApp;

namespace UnitTest
{
    class Program
    {
        static string[] s_testFileNames = new string[]
        {
            "LegacyIdsInUse.txt",
            "ElaLegacyIdsFromCoreStandards.txt",
            "MathLegacyIdsFromCoreStandards.txt",
            "ElaEnhancedIdsFromCASE.txt",
            "MathEnhancedIdsFromCASE.txt"
        };

        static string s_workingDirectory;

        static void Main(string[] args)
        {
            try
            {
                // Locate the working directory in the parent path of the directory containing the executable
                s_workingDirectory = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                while (!File.Exists(Path.Combine(s_workingDirectory, s_testFileNames[0])))
                {
                    s_workingDirectory = Path.GetDirectoryName(s_workingDirectory);
                    if (s_workingDirectory == null)
                    {
                        throw new ApplicationException($"Unable to find working directory containing '{s_testFileNames[0]}' in the parent path of the executable '{Assembly.GetEntryAssembly().Location}'.");
                    }
                }

                Console.WriteLine($"Working directory: {s_workingDirectory}");

                foreach(string filename in s_testFileNames)
                {
                    TestIdList(Path.Combine(s_workingDirectory, filename));
                }

            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
#endif
        }

        static bool TestIdList(string path)
        {
            Console.WriteLine($"Testing IDs in '{path}'.");

            int idCount = 0;
            int errorCount = 0;
            int legacyFormatCount = 0;
            int enhancedFormatCount = 0;
            using (var idFile = new StreamReader(path))
            {
                for (; ; )
                {
                    // Read one input line
                    string line = idFile.ReadLine();
                    if (line == null) break;
                    line = line.Trim();
                    if (line.Length == 0) continue;

                    // Separate the grade field if present
                    var grade = ContentSpecGrade.Unspecified;
                    int space = line.IndexOf(' ');
                    if (space > 0)
                    {
                        grade = ContentSpecId.ParseGrade(line.Substring(0, space));
                        line = line.Substring(space + 1);
                    }

                    ++idCount;

                    switch (TestId(line, grade))
                    {
                        case ContentSpecIdFormat.Enhanced:
                            ++enhancedFormatCount;
                            break;

                        case ContentSpecIdFormat.ElaV1:
                        case ContentSpecIdFormat.MathV4:
                        case ContentSpecIdFormat.MathV5:
                        case ContentSpecIdFormat.MathV6:
                            ++legacyFormatCount;
                            break;

                        default:
                            ++errorCount;
                            break;
                    }
                }
            }

            // Report results
            Console.WriteLine($"{idCount,4} Ids Tested");
            Console.WriteLine($"{enhancedFormatCount,4} Enhanced Ids");
            Console.WriteLine($"{legacyFormatCount,4} Legacy Ids");
            Console.WriteLine($"{errorCount,4} Errors Reported");
            Console.WriteLine();

            return errorCount == 0;
        }

        static ContentSpecIdFormat TestId(string strId, ContentSpecGrade defaultGrade)
        {
            try
            {
                // Parse the ID and check for errors
                var id = ContentSpecId.TryParse(strId, defaultGrade);
                if (id.ParseErrorSeverity != ErrorSeverity.NoError)
                {
                    // Report a parse error
                    WriteLine(strId);
                    WriteLine(id.ParseErrorSeverity == ErrorSeverity.Corrected ? ConsoleColor.Green : ConsoleColor.Red,
                        $"   {id.ParseErrorDescription}");
                    return ContentSpecIdFormat.Unknown;
                }

                // Check for format-ability
                if (id.ValidateFor(id.ParseFormat) != ErrorSeverity.NoError)
                {
                    // Report a validation error
                    WriteLine(strId);
                    WriteLine(ConsoleColor.Red, $"   {id.ValidationErrorDescription}");
                    return ContentSpecIdFormat.Unknown;
                }

                // Make sure the comparison ID has grade in it
                string compId = (id.ParseFormat != ContentSpecIdFormat.Enhanced)
                    ? RemedyMissingGrade(strId, defaultGrade)
                    : strId;

                // Check for round-trip match
                string roundTrip = id.ToString();
                if (!string.Equals(compId, roundTrip, StringComparison.OrdinalIgnoreCase))
                {
                    WriteLine(strId);
                    WriteLine(ConsoleColor.Red, $"   ID doesn't match: {roundTrip}");
                    return ContentSpecIdFormat.Unknown;
                }

                // If claim or target is not specfied then it cannot be converted to
                // legacy format. Do not perform the test.
                if (id.ParseFormat == ContentSpecIdFormat.Enhanced
                    && (id.Claim == ContentSpecClaim.Unspecified || string.IsNullOrEmpty(id.Target)))
                    return id.ParseFormat;

                // Format legacy as enhnaced and enhanced as legacy
                ContentSpecIdFormat convertFormat;
                switch (id.ParseFormat)
                {
                    case ContentSpecIdFormat.ElaV1:
                    case ContentSpecIdFormat.MathV4:
                    case ContentSpecIdFormat.MathV5:
                    case ContentSpecIdFormat.MathV6:
                        convertFormat = ContentSpecIdFormat.Enhanced;
                        break;

                    case ContentSpecIdFormat.Enhanced:
                        switch (id.Subject)
                        {
                            case ContentSpecSubject.ELA:
                                convertFormat = ContentSpecIdFormat.ElaV1;
                                break;

                            case ContentSpecSubject.Math:
                                convertFormat = ContentSpecIdFormat.MathV4;
                                break;

                            default:
                                throw new ApplicationException("Unexpected subject.");
                        }
                        break;

                    default:
                        throw new ApplicationException("Unexpected format.");
                }

                // See if can be reformatted to target format
                if (id.ValidateFor(convertFormat) != ErrorSeverity.NoError)
                {
                    WriteLine(strId);
                    WriteLine(ConsoleColor.Red, $"   Cannot convert from '{id.ParseFormat} to '{convertFormat}': {id.ValidationErrorDescription}");
                    return ContentSpecIdFormat.Unknown;
                }

                // Round-trip through conversion format
                var convertedIdStr = id.ToString(convertFormat);
                var convertedId = ContentSpecId.TryParse(convertedIdStr);
                if (!convertedId.ParseSucceeded)
                {
                    WriteLine(strId);
                    WriteLine(ConsoleColor.Red, $"   {convertedIdStr}");
                    WriteLine(ConsoleColor.Red, $"   Failed to parse converted format: {convertedId.ParseErrorDescription}");
                    return ContentSpecIdFormat.Unknown;
                }

                if (!id.Equals(convertedId))
                {
                    WriteLine(strId);
                    WriteLine(ConsoleColor.Red, $"   {convertedIdStr}");
                    WriteLine(ConsoleColor.Red, $"   Converted ID is not equal to original.");
                    return ContentSpecIdFormat.Unknown;
                }

                if (convertedId.ValidateFor(id.ParseFormat) != ErrorSeverity.NoError)
                {
                    WriteLine(strId);
                    WriteLine(ConsoleColor.Red, $"   {convertedIdStr}");
                    WriteLine(ConsoleColor.Red, $"   Cannot convert from '{convertedId.ParseFormat}'to original '{id.ParseFormat}': ${convertedId.ValidationErrorDescription}");
                    return ContentSpecIdFormat.Unknown;
                }

                roundTrip = convertedId.ToString(id.ParseFormat);
                if (!string.Equals(compId, roundTrip, StringComparison.OrdinalIgnoreCase))
                {
                    WriteLine(strId);
                    WriteLine(ConsoleColor.Red, $"   {convertedIdStr}");
                    WriteLine(ConsoleColor.Red, $"   ID doesn't match when round-tripped through alternative format: {roundTrip}");
                    return ContentSpecIdFormat.Unknown;
                }

                return id.ParseFormat;
            }
            catch (Exception)
            {
                WriteLine(ConsoleColor.Red, strId);
                throw;
            }
        }

        // Legacy IDs frequently are missing the grade. This method adds the grade back
        // so that we can compare it with the round-tripped identifier.
        static string RemedyMissingGrade(string id, ContentSpecGrade grade)
        {
            int colon = id.IndexOf(':');
            if (colon <= 0)
            {
                return id;
            }

            int targetPart = -1;
            switch (id.Substring(0, colon))
            {
                case "SBAC-ELA-v1":
                    targetPart = 1;
                    break;
                case "SBAC-MA-v4":
                case "SBAC-MA-v5":
                    targetPart = 2;
                    break;

                case "SBAC-MA-v6":
                    targetPart = 3;
                    break;
                default:
                    return id;
            }

            string[] parts = id.Split('|');
            if (parts[targetPart].IndexOf('-') > 0)
            {
                return id;
            }

            parts[targetPart] = $"{parts[targetPart]}-{(int)grade}";
            return string.Join('|', parts);
        }

        static void ExtractIdsFromCASE(string inputPath, string outputPath)
        {
            // Read input from JSON into XML
            System.Xml.XPath.XPathDocument doc;
            using (var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = System.Runtime.Serialization.Json.JsonReaderWriterFactory.CreateJsonReader(stream, new System.Xml.XmlDictionaryReaderQuotas()))
                {
                    doc = new System.Xml.XPath.XPathDocument(reader);
                }
            }

            using (var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
            {
                foreach (System.Xml.XPath.XPathNavigator node in doc.CreateNavigator().Select("//humanCodingScheme"))
                {
                    writer.WriteLine(node.Value);
                }
            }
        }

        static void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        static void WriteLine(ConsoleColor color, string text)
        {
            var save = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = save;
        }
    }
}