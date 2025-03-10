// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Pdf;

namespace Docfx;

/// <summary>
/// Provides access to a set of documentations
/// and their associated configs, compilations and models.
/// </summary>
public static class Docset
{
    /// <summary>
    /// Builds a docset specified by docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task Build(string configPath)
    {
        return Build(configPath, new());
    }

    /// <summary>
    /// Builds a docset specified by docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <param name="options">The build options.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task Build(string configPath, BuildOptions options)
    {
        return Exec(configPath, (config, configDirectory) => RunBuild.Exec(config.build, options, configDirectory));
    }

    /// <summary>
    /// Builds a pdf specified by docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task Pdf(string configPath)
    {
        return Pdf(configPath, new());
    }

    /// <summary>
    /// Builds a pdf specified by docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <param name="options">The build options.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task Pdf(string configPath, BuildOptions options)
    {
        return Exec(configPath, (config, configDirectory) =>
        {
            if (config.build is not null)
                PdfBuilder.Run(config.build, configDirectory);

            if (config.pdf is not null)
                RunPdf.Exec(config.pdf, options, configDirectory);
        });
    }

    internal static (DocfxConfig, string configDirectory) GetConfig(string configFile)
    {
        if (string.IsNullOrEmpty(configFile))
            configFile = DataContracts.Common.Constants.ConfigFileName;

        configFile = Path.GetFullPath(configFile);

        if (!File.Exists(configFile))
            throw new FileNotFoundException($"Cannot find config file {configFile}");

        var config = JsonUtility.Deserialize<DocfxConfig>(configFile);

        Logger.Rules = config.rules;

        return (config, Path.GetDirectoryName(configFile));
    }


    private static Task Exec(string configPath, Action<DocfxConfig, string> action)
    {
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        try
        {
            var (config, baseDirectory) = GetConfig(configPath);
            action(config, baseDirectory);
            return Task.CompletedTask;
        }
        finally
        {
            Logger.Flush();
            Logger.PrintSummary();
            Logger.UnregisterAllListeners();
        }
    }
}
