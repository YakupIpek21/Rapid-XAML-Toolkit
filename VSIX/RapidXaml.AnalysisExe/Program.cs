﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using RapidXamlToolkit;
using RapidXamlToolkit.XamlAnalysis;
using RapidXamlToolkit.XamlAnalysis.Tags;

namespace RapidXaml.AnalysisExe
{
    public class Program
    {
#pragma warning disable SA1310 // Field names should not contain underscore
        private const int ERRORCODE_FILE_PATH_NOT_SPECIFIED = 1;
        private const int ERRORCODE_FILE_DOES_NOT_EXIST = 2;
        private const int ERRORCODE_NOT_A_PROJECT_FILE = 3;
#pragma warning restore SA1310 // Field names should not contain underscore

        public static void Main(string[] args)
        {
            try
            {
                // Output name and version for help in debugging any issues.
                Console.WriteLine($"{System.Reflection.Assembly.GetExecutingAssembly().FullName}");

                if (args.Length < 1)
                {
                    Console.WriteLine($"ERROR. Expecting project file path as command line argument.");
                    Environment.ExitCode = ERRORCODE_FILE_PATH_NOT_SPECIFIED;
                    return;
                }

                var projectPath = args[0];

                Console.WriteLine($"Analyzing {projectPath}");

                if (!File.Exists(projectPath))
                {
                    Console.WriteLine($"ERROR. File does not exist.");
                    Environment.ExitCode = ERRORCODE_FILE_DOES_NOT_EXIST;
                    return;
                }

                var fileExt = Path.GetExtension(projectPath);

                if (!fileExt.ToLowerInvariant().Equals(".csproj")
                  & !fileExt.ToLowerInvariant().Equals(".vbproj"))
                {
                    Console.WriteLine($"ERROR. Not a supported project file.");
                    Environment.ExitCode = ERRORCODE_NOT_A_PROJECT_FILE;
                    return;
                }

                var projFileLines = File.ReadAllLines(projectPath);
                var projDir = Path.GetDirectoryName(projectPath);

                // Ensure any errors are output - otherwise debugging problems is really hard
                SharedRapidXamlPackage.Logger = new AnalysisExeLogger();

                // Treat project type as unknown as unable to resolve referenced projects and installed NuGet packages.
                var bavsa = new BuildAnalysisVisualStudioAbstraction(projectPath, ProjectType.Unknown);

                var linesOutputted = new List<string>();

                var files = GetAllXamlFilesInCurrentDir(projDir);

                foreach (var xamlFilePath in files)
                {
                    Console.WriteLine($"- Analyzing: '{xamlFilePath}'");

                    var snapshot = new BuildAnalysisTextSnapshot(xamlFilePath);
                    var rxdoc = RapidXamlDocument.Create(snapshot, xamlFilePath, bavsa, projectPath);

                    Debug.WriteLine($"Found {rxdoc.Tags.Count} taggable issues in '{xamlFilePath}'.");

                    if (rxdoc.Tags.Count > 0)
                    {
                        var tagsOfInterest = rxdoc.Tags
                                                  .Where(t => t is RapidXamlDisplayedTag)
                                                  .Select(t => t as RapidXamlDisplayedTag)
                                                  .ToList();

                        Debug.WriteLine($"Found {tagsOfInterest.Count} issues to report in '{xamlFilePath}'.");

                        foreach (var issue in tagsOfInterest)
                        {
                            string messageType = string.Empty;
                            switch (issue.ConfiguredErrorType)
                            {
                                case TagErrorType.Error:
                                    messageType = "error";
                                    break;
                                case TagErrorType.Warning:
                                    messageType = "warning";
                                    break;
                                case TagErrorType.Suggestion:
                                    // Not supported in the build process
                                    break;
                                case TagErrorType.Hidden:
                                    break;
                            }

                            if (!string.IsNullOrEmpty(messageType))
                            {
                                // Add 1 to line number to allow for VS counting without zero index
                                // Error code is repeated with the description because it doesn't show in the Visual Studio Error List
                                // For format see https://github.com/Microsoft/msbuild/blob/master/src/Shared/CanonicalError.cs

                                //var outputText = $"{xamlFilePath}({issue.Line + 1},{issue.Column}): {messageType} {issue.ErrorCode}: {issue.Description} ({issue.ErrorCode})";
                                var outputText = $"{xamlFilePath}({issue.Line + 1},{issue.Column}): {messageType} {issue.ErrorCode}: {issue.Description} (BuildAnaylsis Output)";

                                if (!linesOutputted.Contains(outputText))
                                {
                                    linesOutputted.Add(outputText);
                                    Console.WriteLine(outputText);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine($"ERROR. {exc}");
            }
        }

        private static IEnumerable<string> GetXamlFiles(string directory)
        {
            var files = Directory.GetFiles(directory);

            foreach (var file in files)
            {
                if (file.EndsWith(".xaml"))
                {
                    yield return file;
                }
            }
        }

        private static IEnumerable<string> GetAllXamlFilesInCurrentDir(string directory)
        {
            if (!Directory.Exists(directory))
            {
                yield break;
            }

            if (directory.Contains("\\obj\\") || directory.Contains("\\bin\\"))
            {
                yield break;
            }

            var directories = Directory.GetDirectories(directory).ToList();

            foreach (var d in directories)
            {
                foreach (var xamlfiles in GetAllXamlFilesInCurrentDir(d))
                {
                    yield return xamlfiles;
                }
            }

            foreach (var xamlFiles in GetXamlFiles(directory))
            {
                yield return xamlFiles;
            }
        }
    }
}
