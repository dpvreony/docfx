﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Docfx.Common;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

#nullable enable

namespace Docfx.Dotnet;

partial class DotnetApiCatalog
{
    private static async Task<List<(IAssemblySymbol symbol, Compilation compilation)>> Compile(ExtractMetadataConfig config, DotnetApiOptions options)
    {
        var files = config.Files?.Select(s => new FileInformation(s))
            .GroupBy(f => f.Type)
            .ToDictionary(s => s.Key, s => s.Distinct().ToList()) ?? new();

        var msbuildProperties = config.MSBuildProperties ?? new Dictionary<string, string>();
        if (!msbuildProperties.ContainsKey("Configuration"))
        {
            msbuildProperties["Configuration"] = "Release";
        }

        var msbuildLogger = new ConsoleLogger(Logger.LogLevelThreshold switch
        {
            LogLevel.Verbose => LoggerVerbosity.Normal,
            LogLevel.Diagnostic => LoggerVerbosity.Diagnostic,
            _ => LoggerVerbosity.Quiet,
        });

        var workspace = MSBuildWorkspace.Create(msbuildProperties);
        workspace.WorkspaceFailed += (sender, e) => Logger.LogWarning($"{e.Diagnostic}");

        if (files.TryGetValue(FileType.NotSupported, out var unsupportedFiles))
        {
            foreach (var file in unsupportedFiles)
            {
                Logger.LogWarning($"Skip unsupported file {file}");
            }
        }

        var hasCompilationError = false;
        var projectCompilations = new HashSet<Compilation>();
        var assemblies = new List<(IAssemblySymbol, Compilation)>();

        if (files.TryGetValue(FileType.Solution, out var solutionFiles))
        {
            foreach (var solution in solutionFiles.Select(s => s.NormalizedPath))
            {
                Logger.LogInfo($"Loading solution {solution}");
                foreach (var project in SolutionFile.Parse(solution).ProjectsInOrder)
                {
                    if (project.ProjectType is SolutionProjectType.KnownToBeMSBuildFormat &&
                        await LoadCompilationFromProject(project.AbsolutePath) is { } compilation)
                    {
                        projectCompilations.Add(compilation);
                    }
                }
            }
        }

        if (files.TryGetValue(FileType.Project, out var projectFiles))
        {
            foreach (var projectFile in projectFiles)
            {
                if (await LoadCompilationFromProject(projectFile.NormalizedPath) is { } compilation)
                {
                    projectCompilations.Add(compilation);
                }
            }
        }

        foreach (var compilation in projectCompilations)
        {
            hasCompilationError |= compilation.CheckDiagnostics(config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        if (files.TryGetValue(FileType.CSSourceCode, out var csFiles))
        {
            var compilation = CompilationHelper.CreateCompilationFromCSharpFiles(csFiles.Select(f => f.NormalizedPath));
            hasCompilationError |= compilation.CheckDiagnostics(config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        if (files.TryGetValue(FileType.VBSourceCode, out var vbFiles))
        {
            var compilation = CompilationHelper.CreateCompilationFromVBFiles(vbFiles.Select(f => f.NormalizedPath));
            hasCompilationError |= compilation.CheckDiagnostics(config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        if (files.TryGetValue(FileType.Assembly, out var assemblyFiles))
        {
            foreach (var assemblyFile in assemblyFiles)
            {
                Logger.LogInfo($"Loading assembly {assemblyFile.NormalizedPath}");
                var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly(assemblyFile.NormalizedPath, config.References);
                hasCompilationError |= compilation.CheckDiagnostics(config.AllowCompilationErrors);
                assemblies.Add((assembly, compilation));
            }
        }

        if (hasCompilationError)
        {
            return new();
        }

        if (assemblies.Count <= 0)
        {
            Logger.LogWarning("No .NET API project detected.");
        }

        return assemblies;

        async Task<Compilation?> LoadCompilationFromProject(string path)
        {
            var project = workspace.CurrentSolution.Projects.FirstOrDefault(
                p => FilePathComparer.OSPlatformSensitiveRelativePathComparer.Equals(p.FilePath, path));

            if (project is null)
            {
                Logger.LogInfo($"Loading project {path}");
                if (!config.NoRestore)
                {
                    await Process.Start("dotnet", $"restore \"{path}\"").WaitForExitAsync();
                }
                project = await workspace.OpenProjectAsync(path, msbuildLogger);
            }

            if (!project.SupportsCompilation)
            {
                Logger.LogInfo($"Skip unsupported project {project.FilePath}.");
                return null;
            }

            return await project.GetCompilationAsync();
        }
    }
}
