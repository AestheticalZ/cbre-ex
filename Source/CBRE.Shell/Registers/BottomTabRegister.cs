﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogicAndTrick.Oy;
using CBRE.Common.Logging;
using CBRE.Common.Shell.Components;
using CBRE.Common.Shell.Context;
using CBRE.Common.Shell.Hooks;

namespace CBRE.Shell.Registers
{
    /// <summary>
    /// The bottom register controls bottom tabs
    /// </summary>
    [Export(typeof(IStartupHook))]
    [Export(typeof(IInitialiseHook))]
    public class BottomTabRegister : IStartupHook, IInitialiseHook
    {
        // The bottom tab register needs direct access to the shell
        private readonly Lazy<Forms.Shell> _shell;
        private readonly IEnumerable<Lazy<IBottomTabComponent>> _bottomTabComponents;

        private readonly List<IBottomTabComponent> _components;

        [ImportingConstructor]
        internal BottomTabRegister(
            [Import] Lazy<Forms.Shell> shell,
            [ImportMany] IEnumerable<Lazy<IBottomTabComponent>> bottomTabComponents
        )
        {
            _shell = shell;
            _bottomTabComponents = bottomTabComponents;

            _components = new List<IBottomTabComponent>();
        }

        public Task OnStartup()
        {
            // Register the exported sidebar components
            foreach (Lazy<IBottomTabComponent> export in _bottomTabComponents)
            {
                Log.Debug("Bottom tabs", "Loaded: " + export.Value.GetType().FullName);
                _components.Add(export.Value);
            }

            Initialise();

            // Subscribe to context changes
            Oy.Subscribe<IContext>("Context:Changed", ContextChanged);

            return Task.CompletedTask;
        }

        public Task OnInitialise()
        {
            _shell.Value.InvokeLater(() =>
            {
                foreach (TabPage tab in _shell.Value.BottomTabs.TabPages.OfType<TabPage>())
                {
                    if (!(tab.Tag is IBottomTabComponent btc)) continue;
                    tab.Text = btc.Title;
                }
            });

            return Task.CompletedTask;
        }

        private void Initialise()
        {
            _shell.Value.Invoke((MethodInvoker)delegate
            {
                _shell.Value.BottomTabs.TabPages.Clear();
                foreach (IBottomTabComponent btc in _components)
                {
                    TabPage page = new TabPage(btc.Title) { Tag = btc, Visible = false };
                    page.Controls.Add((Control) btc.Control);
                    _shell.Value.BottomTabs.TabPages.Add(page);
                }
            });
        }

        private Task ContextChanged(IContext context)
        {
            _shell.Value.Invoke((MethodInvoker) delegate
            {
                _shell.Value.BottomTabs.SuspendLayout();
                foreach (TabPage tab in _shell.Value.BottomTabs.TabPages.OfType<TabPage>())
                {
                    IBottomTabComponent btc = tab.Tag as IBottomTabComponent;
                    if (btc == null) continue;

                    bool iic = btc.IsInContext(context);
                    bool vis = tab.Visible;
                    tab.Text = btc.Title;

                    if (iic != vis) tab.Visible = iic;
                }
                _shell.Value.BottomTabs.ResumeLayout();
            });
            return Task.CompletedTask;
        }
    }
}