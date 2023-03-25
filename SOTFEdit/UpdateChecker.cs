﻿using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json.Linq;
using NLog;
using Semver;
using SOTFEdit.Model;
using SOTFEdit.Model.Events;

namespace SOTFEdit;

public class UpdateChecker
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly SemVersion _assemblyVersion;
    private readonly HttpClient _client;
    private readonly string _latestTagUrl;
    private volatile bool _isLoading;

    public UpdateChecker(GameData gameData)
    {
        _latestTagUrl = gameData.Config.LatestTagUrl;
        var assemblyInfo = Assembly.GetExecutingAssembly()
            .GetName();
        _assemblyVersion = SemVersion.FromVersion(assemblyInfo.Version);

        _client = new HttpClient
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", $"{assemblyInfo.Name}/{_assemblyVersion}" }
            }
        };
    }

    public void CheckForUpdates(bool notifyOnSameVersion, bool notifyOnError, bool invokedManually)
    {
        Task.Run(() => DoCheckForUpdates(notifyOnSameVersion, notifyOnError, invokedManually));
    }

    private async Task DoCheckForUpdates(bool notifyOnSameVersion, bool notifyOnError, bool invokedManually)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _latestTagUrl);

            using var result = await _client.SendAsync(request);
            if (!result.IsSuccessStatusCode)
            {
                Logger.Warn($"Got a failure response: {result.StatusCode}");
                NotifyOnError(notifyOnError, invokedManually);
                return;
            }

            var json = await result.Content.ReadAsStringAsync();

            var doc = JToken.Parse(json);
            if (doc["tag_name"]?.ToString() is not { } tagName)
            {
                Logger.Warn("Tag version not found in response");
                NotifyOnError(notifyOnError, invokedManually);
                return;
            }

            var latestTagVersion = SemVersion.Parse(tagName, SemVersionStyles.AllowV);
            switch (latestTagVersion.ComparePrecedenceTo(_assemblyVersion))
            {
                case 1:
                    var link = doc["html_url"]?.ToString();
                    NotifyOnNewerVersion(latestTagVersion, link, invokedManually);
                    break;
                case 0 or -1 when notifyOnSameVersion:
                    NotifyOnSameVersion(latestTagVersion, invokedManually);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error while checking for updates");
            NotifyOnError(notifyOnError, invokedManually);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static void NotifyOnSameVersion(SemVersion latestTagVersion, bool invokedManually)
    {
        Logger.Info("No update found");
        SendResult(new VersionCheckResultEvent(latestTagVersion, false, invokedManually));
    }

    private static void NotifyOnNewerVersion(SemVersion latestTagVersion, string? link, bool invokedManually)
    {
        Logger.Info($"Newer version found ({latestTagVersion})");
        SendResult(new VersionCheckResultEvent(latestTagVersion, true, invokedManually)
        {
            Link = link
        });
    }

    private static void NotifyOnError(bool notifyOnError, bool invokedManually)
    {
        if (notifyOnError)
        {
            SendResult(new VersionCheckResultEvent(null, false, invokedManually, true));
        }
    }

    private static void SendResult(VersionCheckResultEvent checkResult)
    {
        _ = Task.Run(() => WeakReferenceMessenger.Default.Send(checkResult));
    }
}