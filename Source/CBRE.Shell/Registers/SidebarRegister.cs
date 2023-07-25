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
using CBRE.Common.Shell.Settings;
using CBRE.Shell.Controls;

namespace CBRE.Shell.Registers
{
    /// <summary>
    /// The sidebar register controls sidebar components, positioning, and visibility
    /// </summary>
    [Export(typeof(IStartupHook))]
    [Export(typeof(IInitialiseHook))]
    [Export(typeof(ISettingsContainer))]
    public class SidebarRegister : IStartupHook, IInitialiseHook, ISettingsContainer
    {
        private readonly Lazy<Form> _shell;
        private readonly IEnumerable<Lazy<ISidebarComponent>> _sidebarComponents;

        private List<SidebarComponent> _left;
        private List<SidebarComponent> _right;

        private Forms.Shell Shell => (Forms.Shell) _shell.Value;

        [ImportingConstructor]
        public SidebarRegister(
            [Import("Shell")] Lazy<Form> shell,
            [ImportMany] IEnumerable<Lazy<ISidebarComponent>> sidebarComponents
        )
        {
            _shell = shell;
            _sidebarComponents = sidebarComponents;

            _left = new List<SidebarComponent>();
            _right = new List<SidebarComponent>();
        }

        public Task OnStartup()
        {
            // Register the exported sidebar components
            foreach (Lazy<ISidebarComponent> export in _sidebarComponents)
            {
                Type ty = export.Value.GetType();
                string hint = OrderHintAttribute.GetOrderHint(ty);
                Add(export.Value, hint);
                Log.Debug("Sidebar", "Loaded: " + export.Value.GetType().FullName);
            }

            // Subscribe to context changes
            Oy.Subscribe<IContext>("Context:Changed", ContextChanged);
            return Task.CompletedTask;
        }

        public Task OnInitialise()
        {
            _left.ForEach(x => x.UpdateTitle());
            _right.ForEach(x => x.UpdateTitle());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Add a sidebar component to the right sidebar
        /// </summary>
        /// <param name="component">The component to add</param>
        /// <param name="orderHint"></param>
        private void Add(ISidebarComponent component, string orderHint)
        {
            SidebarComponent sc = new SidebarComponent(component, orderHint);
            _right.Add(sc);
            _right = _right.OrderBy(x => x.OrderHint).ToList();
            Shell.RightSidebarContainer.Insert(sc.Panel, _right.IndexOf(sc));
        }

        private Task ContextChanged(IContext context)
        {
            foreach (SidebarComponent sc in _left.Union(_right))
            {
                sc.ContextChanged(context);
            }

            return Task.CompletedTask;
        }

        // Settings provider
        // The settings provider is what moves the sidebar components between left and right.
        // If no settings exist, they'll sit in the right sidebar by default.

        public string Name => "CBRE.Shell.Sidebar";

        public IEnumerable<SettingKey> GetKeys()
        {
            yield break;
        }

        public void LoadValues(ISettingsStore store)
        {
            _shell.Value.Invoke((MethodInvoker) delegate
            {
                Dictionary<string, SidebarComponent> controls = _left.Union(_right).ToDictionary(x => x.ID, x => x);
                foreach (string sv in store.GetKeys())
                {
                    if (sv.EndsWith(":Side"))
                    {
                        string key = sv.Substring(0, sv.Length - 5);
                        if (controls.ContainsKey(key))
                        {
                            SidebarComponent con = controls[key];

                            _left.Remove(con);
                            _right.Remove(con);

                            if (store.Get(sv, "Left") == "Right") _right.Add(con);
                            else _left.Add(con);
                        }
                    }
                    if (sv.EndsWith(":Expanded"))
                    {
                        string key = sv.Substring(0, sv.Length - 9);
                        if (controls.ContainsKey(key))
                        {
                            SidebarComponent con = controls[key];
                            con.Panel.Hidden = !store.Get(sv, true);
                        }
                    }
                    if (sv.EndsWith(":Order"))
                    {

                    }
                }
            });
        }

        public void StoreValues(ISettingsStore store)
        {
            for (int i = 0; i < _left.Count; i++)
            {
                SidebarComponent sc = _left[i];
                store.Set($"{sc.ID}:Side", "Left");
                store.Set($"{sc.ID}:Order", i);
                store.Set($"{sc.ID}:Expanded", !sc.Panel.Hidden);
            }
            for (int i = 0; i < _right.Count; i++)
            {
                SidebarComponent sc = _right[i];
                store.Set($"{sc.ID}:Side", "Right");
                store.Set($"{sc.ID}:Order", i);
                store.Set($"{sc.ID}:Expanded", !sc.Panel.Hidden);
            }
        }

        /// <summary>
        /// A container for a sidebar component.
        /// </summary>
        private class SidebarComponent
        {
            public string OrderHint { get; }

            /// <summary>
            /// The source component
            /// </summary>
            public ISidebarComponent Component { get; private set; }

            /// <summary>
            /// The container panel
            /// </summary>
            public SidebarPanel Panel { get; private set; }

            /// <summary>
            /// The component ID
            /// </summary>
            public string ID => Component.GetType().FullName;

            public SidebarComponent(ISidebarComponent component, string orderHint)
            {
                OrderHint = orderHint ?? "T";
                Component = component;
                Panel = new SidebarPanel
                {
                    Text = component.Title,
                    Name = component.Title,
                    Dock = DockStyle.Fill,
                    Hidden = false,
                    Visible = false,
                    Tag = this
                };
                Panel.AddControl((Control) component.Control);
            }

            public void UpdateTitle()
            {
                Panel.InvokeLater(() =>
                {
                    Panel.Text = Component.Title;
                });
            }

            /// <summary>
            /// Update the component visibility based on the current context
            /// </summary>
            /// <param name="context">The current context</param>
            public void ContextChanged(IContext context)
            {
                Panel.InvokeLater(() =>
                {
                    bool iic = Component.IsInContext(context);
                    Panel.Text = Component.Title;
                    if (iic != Panel.Visible) Panel.Visible = iic;
                });
            }
        }
    }
}
