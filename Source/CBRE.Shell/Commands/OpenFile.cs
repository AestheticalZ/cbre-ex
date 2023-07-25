﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogicAndTrick.Oy;
using CBRE.Common.Shell.Commands;
using CBRE.Common.Shell.Context;
using CBRE.Common.Shell.Documents;
using CBRE.Common.Shell.Hotkeys;
using CBRE.Common.Shell.Menu;
using CBRE.Common.Translations;
using CBRE.Shell.Properties;

namespace CBRE.Shell.Commands
{
    [AutoTranslate]
    [Export(typeof(ICommand))]
    [CommandID("File:Open")]
    [DefaultHotkey("Ctrl+O")]
    [MenuItem("File", "", "File", "D")]
    [MenuImage(typeof(Resources), nameof(Resources.Menu_Open))]
    public class OpenFile : ICommand
    {
        private readonly IEnumerable<Lazy<IDocumentLoader>> _loaders;

        public string Name { get; set; } = "Open";
        public string Details { get; set; } = "Open...";

        [ImportingConstructor]
        public OpenFile([ImportMany] IEnumerable<Lazy<IDocumentLoader>> loaders)
        {
            _loaders = loaders;
        }

        public bool IsInContext(IContext context)
        {
            return true;
        }

        public async Task Invoke(IContext context, CommandParameters parameters)
        {
            List<string> filter = _loaders.Select(x => x.Value).Select(x => x.FileTypeDescription + "|" + string.Join(";", x.SupportedFileExtensions.SelectMany(e => e.Extensions).Select(e => "*" + e))).ToList();
            filter.Add("All files|*.*");
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = string.Join("|", filter)})
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;

                await Oy.Publish("Command:Run", new CommandMessage("Internal:OpenDocument", new
                {
                    Path = ofd.FileName
                }));
            }
        }
    }
}
