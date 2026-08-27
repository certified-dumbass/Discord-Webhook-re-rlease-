using System;
using System.Collections.Generic;
using System.IO;
using Dreamstreaming.DiscordBot.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Dreamstreaming.DiscordBot;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly IApplicationPaths _applicationPaths;

    public static Plugin? Instance { get; private set; }

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        _applicationPaths = applicationPaths;
        Instance = this;
    }

    public override string Name => "Dreamstreaming Discord Bot";

    public override Guid Id =>
        Guid.Parse("7B7E6E2A-5F2B-4A3D-9F64-9D0E3C6D8A21");

    /// <summary>
    /// Folder for runtime state such as lastscan.json and scheduler-state.json.
    /// This lives in Jellyfin's plugin configuration area instead of the plugin
    /// installation directory so it remains writable and survives updates.
    /// </summary>
    public string StateDirectory =>
        Path.Combine(
            _applicationPaths.PluginConfigurationsPath,
            "Dreamstreaming.DiscordBot");

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "DreamstreamingDiscordBotConfiguration",
            DisplayName = "Dreamstreaming Discord Bot",
            EmbeddedResourcePath =
                "Dreamstreaming.DiscordBot.Web.config.html"
        };
    }
}
