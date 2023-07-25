﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System;
using System.Globalization;
using CBRE.Common.Shell.Settings;

namespace CBRE.Shell.Settings
{
    /// <summary>
    /// Manages layout settings for the shell in general.
    /// Remembers the window state and dockpanel sizes.
    /// </summary>
    [Export(typeof(ISettingsContainer))]
    public class LayoutSettingsContainer : ISettingsContainer
    {
        [Import] private Forms.Shell _shell;
        
        // Settings provider

        public string Name => "CBRE.Shell.Layout";

        public IEnumerable<SettingKey> GetKeys()
        {
            yield break;
        }

        public void LoadValues(ISettingsStore store)
        {
            _shell.Invoke((MethodInvoker) delegate
            {
                _shell.WindowState = store.Get("WindowState", _shell.WindowState);

                foreach (Controls.DockedPanel dp in _shell.GetDockPanels())
                {
                    string dph = $"DockPanel:{dp.Name}:Hidden";
                    string dps = $"DockPanel:{dp.Name}:Size";
                    dp.Hidden = store.Get(dph, dp.Hidden);
                    dp.DockDimension = store.Get(dps, dp.DockDimension);
                }
            });
        }

        public void StoreValues(ISettingsStore store)
        {
            store.Set("WindowState", Convert.ToString(_shell.WindowState, CultureInfo.InvariantCulture));
            foreach (Controls.DockedPanel dp in _shell.GetDockPanels())
            {
                store.Set($"DockPanel:{dp.Name}:Hidden", dp.Hidden);
                store.Set($"DockPanel:{dp.Name}:Size", dp.DockDimension);
            }
        }
    }
}