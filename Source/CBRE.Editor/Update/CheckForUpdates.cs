﻿using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using CBRE.Common.Shell.Commands;
using CBRE.Common.Shell.Context;
using CBRE.Common.Shell.Menu;
using CBRE.Common.Translations;
using CBRE.Editor.Properties;
using CBRE.Shell;

namespace CBRE.Editor.Update
{
    [AutoTranslate]
    [Export(typeof(ICommand))]
    [MenuItem("Help", "", "Update", "B")]
    [CommandID("CBRE:Editor:CheckForUpdates")]
    [MenuImage(typeof(Resources), nameof(Resources.Menu_Update))]
    public class CheckForUpdates : ICommand
    {
        private readonly Form _shell;
        private readonly ITranslationStringProvider _translation;

        public string Name { get; set; } = "Check for updates";
        public string Details { get; set; } = "Check online for updates.";

        public string NoUpdatesTitle { get; set; } = "No updates found";
        public string NoUpdatesMessage { get; set; } = "This version of CBRE-EX is currently up-to-date.";

        public string UpdateErrorTitle { get; set; } = "Update error";
        public string UpdateErrorMessage { get; set; } = "Error downloading the update details.";

        private const string GithubReleasesApiUrl = "https://api.github.com/repos/LogicAndTrick/sledge/releases?page=1&per_page=1";
        private const string SledgeWebsiteUpdateSource = "http://sledge-editor.com/version.txt";

        [ImportingConstructor]
        public CheckForUpdates(
            [Import("Shell")] Form shell,
            [Import] ITranslationStringProvider translation
        )
        {
            _shell = shell;
            _translation = translation;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public bool IsInContext(IContext context)
        {
            return true;
        }

        public async Task Invoke(IContext context, CommandParameters parameters)
        {
            bool silent = parameters.Get("Silent", false);

            UpdateCheckResult result = await GetUpdateCheckResult(SledgeWebsiteUpdateSource);
            if (result == null) return;

            Version version = GetCurrentVersion();
            if (result.Version <= version)
            {
                if (!silent)
                {
                    _shell.InvokeLater(() =>
                    {
                        MessageBox.Show(NoUpdatesMessage, NoUpdatesTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                return;
            }

            UpdateReleaseDetails details = await GetLatestReleaseDetails();
            if (!details.Exists)
            {
                if (!silent)
                {
                    _shell.InvokeLater(() =>
                    {
                        MessageBox.Show(UpdateErrorMessage, UpdateErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
                return;
            }

            _shell.InvokeLater(() =>
            {
                UpdaterForm form = new UpdaterForm(details, _translation);
                form.Show(_shell);
            });
        }

        private Version GetCurrentVersion()
        {
            return typeof(Program).Assembly.GetName().Version;
        }

        private async Task<UpdateCheckResult> GetUpdateCheckResult(string url)
        {
            try
            {
                using (WebClient downloader = new WebClient())
                {
                    string[] str = (await downloader.DownloadStringTaskAsync(url)).Split('\n', '\r');

                    if (str.Length < 2 || string.IsNullOrWhiteSpace(str[0]))
                    {
                        return null;
                    }

                    return new UpdateCheckResult
                    {
                        Version = new Version(str[0]),
                        Date = DateTime.Parse(str[1])
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<UpdateReleaseDetails> GetLatestReleaseDetails()
        {
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add(HttpRequestHeader.UserAgent, "LogicAndTrick/Sledge-Editor");
                string str = await wc.DownloadStringTaskAsync(GithubReleasesApiUrl);
                return new UpdateReleaseDetails(str);
            }
        }

        private class UpdateCheckResult
        {
            public Version Version { get; set; }
            public DateTime Date { get; set; }
        }
    }
}