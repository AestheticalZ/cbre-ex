﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CBRE.BspEditor.Compile;
using CBRE.BspEditor.Editing.Components.Compile.Profiles;
using CBRE.BspEditor.Editing.Components.Compile.Specification;
using CBRE.Common.Translations;
using CBRE.QuickForms;
using CBRE.Shell;

namespace CBRE.BspEditor.Editing.Components.Compile
{
    public sealed partial class CompileDialog : Form, IManualTranslate
    {
        private CompileSpecification _specification;
        private readonly BuildProfileRegister _buildProfileRegister;

        private CompilePreset _preset;
        private BuildProfile _profile;

        private Size _simpleSize = new Size(320, 450);
        private Size _advancedSize = new Size(750, 550);

        public CompileDialog(CompileSpecification specification, BuildProfileRegister buildProfileRegister)
        {
            InitializeComponent();

            _specification = specification;
            _buildProfileRegister = buildProfileRegister;

            // Hide the panels
            AdvancedPanel.Size = Size.Empty;
            SimplePanel.Size = Size.Empty;

            // Open the default mode
            SimplePanel.Dock = DockStyle.Fill;
            Size = _simpleSize;

            Text = _specification.Name;
            
            PresetTable.Controls.Clear();
            PresetTable.RowStyles.Clear();

            PopulatePresets();
            PopulateTabs();
            PopulateProfiles();

            ITranslationStringProvider translate = CBRE.Common.Container.Get<ITranslationStringProvider>();
            translate.Translate(this);
        }

        public string ProfileName { get; set; }
        public string OK { get; set; }
        public string Cancel { get; set; }
        public string DeleteProfile { get; set; }
        public string DeleteAreYouSure { get; set; }

        public void Translate(ITranslationStringProvider strings)
        {
            CreateHandle();
            string prefix = GetType().FullName;
            this.InvokeLater(() =>
            {
                Text = strings.GetString(prefix, "Title");
                btnAdvancedMode.Text = strings.GetString(prefix, "AdvancedMode");
                lblProfile.Text = strings.GetString(prefix, "Profile");
                tabSteps.Text = strings.GetString(prefix, "StepsToRun");
                btnSimpleMode.Text = strings.GetString(prefix, "SimpleMode");
                btnSaveProfile.Text = strings.GetString(prefix, "SaveProfile");
                btnSaveProfileAs.Text = strings.GetString(prefix, "SaveProfileAs");
                btnCancel.Text = strings.GetString(prefix, "Cancel");
                btnGo.Text = strings.GetString(prefix, "Compile");

                ProfileName = strings.GetString(prefix, "ProfileName");
                OK = strings.GetString(prefix, "OK");
                Cancel = strings.GetString(prefix, "Cancel");
                DeleteProfile = strings.GetString(prefix, "DeleteProfile");
                DeleteAreYouSure = strings.GetString(prefix, "DeleteAreYouSure");
            });
        }

        public IEnumerable<BatchArgument> SelectedBatchArguments
        {
            get
            {
                List<BatchArgument> batch = new List<BatchArgument>();
                if (_preset != null)
                {
                    foreach (CompileTool t in _specification.Tools)
                    {
                        if (_preset.ShouldRunTool(t.Name))
                        {
                            batch.Add(new BatchArgument {Name = t.Name, Arguments = _preset.GetArguments(t.Name)});
                        }
                    }
                }
                else if (_profile != null)
                {
                    Dictionary<string, string> args = _profile.Arguments;

                    string shared = "";
                    if (args.ContainsKey("Shared"))
                    {
                        string sa = args["Shared"];
                        shared = " " + sa;
                    }

                    foreach (KeyValuePair<string, string> kv in args)
                    {
                        batch.Add(new BatchArgument {Name = kv.Key, Arguments = kv.Value + shared});
                    }
                }
                else
                {
                    Dictionary<string, string> args = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (BuildParametersPanel panel in ToolTabs.TabPages.OfType<TabPage>().SelectMany(x => x.Controls.OfType<BuildParametersPanel>()))
                    {
                        args.Add(panel.Tool.Name, panel.Arguments);
                    }

                    string shared = "";
                    if (args.ContainsKey("Shared"))
                    {
                        string sa = args["Shared"];
                        shared = " " + sa;
                    }

                    List<string> shouldRun = new List<string>();
                    foreach (CheckBox step in pnlSteps.Controls.OfType<CheckBox>())
                    {
                        if (step.Checked && step.Tag is CompileTool t) shouldRun.Add(t.Name);
                    }

                    foreach (KeyValuePair<string, string> kv in args)
                    {
                        if (shouldRun.Contains(kv.Key))
                        {
                            batch.Add(new BatchArgument {Name = kv.Key, Arguments = kv.Value + shared});
                        }
                    }
                }
                return batch;
            }
        }

        private void PopulatePresets()
        {
            PresetTable.Controls.Clear();
            foreach (CompilePreset preset in _specification.Presets)
            {
                PresetTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                HeadingButton btn = new HeadingButton
                {
                    HeadingText = preset.Name,
                    Text = preset.Description,
                    Dock = DockStyle.Top
                };
                CompilePreset pre = preset;
                btn.Click += (s, e) => UsePreset(pre);
                PresetTable.Controls.Add(btn);
            }

            foreach (BuildProfile profile in _buildProfileRegister.GetProfiles(_specification.Name))
            {
                Button btn = new Button
                {
                    Text = profile.Name,
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.Click += (s, e) => UseProfile(profile);
                PresetTable.Controls.Add(btn);
            }
        }

        private void PopulateProfiles()
        {
            cmbProfile.Items.Clear();
            cmbProfile.Items.AddRange(_buildProfileRegister.GetProfiles(_specification.Name).Select(x => new ProfileWrapper(x)).ToArray<object>());
            cmbProfile.Items.AddRange(_specification.Presets.Select(x => new ProfileWrapper(x)).ToArray<object>());
            if (cmbProfile.Items.Count > 0) cmbProfile.SelectedIndex = 0;
        }

        private void PopulateTabs()
        {
            pnlSteps.Controls.Clear();
            foreach (TabPage page in ToolTabs.TabPages.OfType<TabPage>().ToList())
            {
                if (page != tabSteps) ToolTabs.TabPages.Remove(page);
            }

            foreach (CompileTool tool in _specification.Tools.OrderBy(x => x.Order))
            {
                if (!string.Equals(tool.Name, "Shared", StringComparison.InvariantCultureIgnoreCase))
                {
                    CheckBox cb = new CheckBox
                    {
                        Text = tool.Name,
                        Tag = tool,
                        Checked = tool.Enabled
                    };
                    pnlSteps.Controls.Add(cb);
                }

                TabPage tab = new TabPage(tool.Name)
                {
                    Tag = tool
                };

                BuildParametersPanel bpp = new BuildParametersPanel
                {
                    Dock = DockStyle.Fill,
                    Tool = tool
                };
                tab.Controls.Add(bpp);

                ToolTabs.TabPages.Add(tab);
            }
        }

        private void UsePreset(CompilePreset preset)
        {
            _preset = preset;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UseProfile(BuildProfile profile)
        {
            _profile = profile;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ProfileSelected(object sender, EventArgs e)
        {
            btnSaveProfile.Enabled = btnRename.Enabled = btnDelete.Enabled = false;
            if (!(cmbProfile.SelectedItem is ProfileWrapper profile)) return;
            UpdateParameters(profile);

            // You can't edit a preset
            btnSaveProfile.Enabled = btnRename.Enabled = btnDelete.Enabled = profile.Preset == null;
        }

        private void UpdateParameters(ProfileWrapper profile)
        {
            List<BuildParametersPanel> panels = ToolTabs.TabPages.OfType<TabPage>().SelectMany(x => x.Controls.OfType<BuildParametersPanel>()).ToList();
            foreach (BuildParametersPanel panel in panels)
            {
                if (string.Equals(panel.Tool.Name, "Shared", StringComparison.InvariantCultureIgnoreCase))
                {
                    panel.Arguments = String.Join(" ", panels.Select(x => profile.GetArguments(x.Tool.Name)));
                }
                else
                {
                    panel.Arguments = profile.GetArguments(panel.Tool.Name);
                }
            }
        }

        private string PromptName(string name)
        {
            QuickForm qf = new QuickForm(ProfileName) {UseShortcutKeys = true};
            qf.TextBox("ProfileName", ProfileName, name);
            qf.OkCancel(OK, Cancel);

            if (qf.ShowDialog() != DialogResult.OK) return null;

            string n = qf.String("ProfileName");
            return String.IsNullOrEmpty(n) ? null : n;
        }

        private void RenameProfileButtonClicked(object sender, EventArgs e)
        {
            if (!(cmbProfile.SelectedItem is ProfileWrapper profile)) return;
            if (profile.Profile == null) return;

            string name = PromptName(profile.GetName());
            if (String.IsNullOrEmpty(name)) return;

            profile.Profile.Name = name;
            
            PopulateProfiles();
            cmbProfile.SelectedIndex = cmbProfile.Items.OfType<ProfileWrapper>().ToList().FindIndex(x => x.Profile == profile.Profile);
        }

        private void DeleteProfileButtonClicked(object sender, EventArgs e)
        {
            if (!(cmbProfile.SelectedItem is ProfileWrapper profile)) return;
            if (profile.Profile == null) return;

            if (MessageBox.Show(DeleteAreYouSure, DeleteProfile, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _buildProfileRegister.Remove(profile.Profile);
                PopulateProfiles();
            }
        }

        private void SaveProfileButtonClicked(object sender, EventArgs e)
        {
            if (!(cmbProfile.SelectedItem is ProfileWrapper profile)) return;
            if (profile.Profile == null) return;

            SetArgumentsFromInterface(profile.Profile);
        }

        private void SaveProfileAsButtonClicked(object sender, EventArgs e)
        {
            string name = PromptName("");
            if (String.IsNullOrEmpty(name)) return;

            BuildProfile profile = new BuildProfile
            {
                Name = name,
                SpecificationName = _specification.Name
            };
            SetArgumentsFromInterface(profile);
            _buildProfileRegister.Add(profile);

            PopulateProfiles();
            cmbProfile.SelectedIndex = cmbProfile.Items.OfType<ProfileWrapper>().ToList().FindIndex(x => x.Profile == profile);
        }

        private void SetArgumentsFromInterface(BuildProfile profile)
        {
            Dictionary<string, string> args = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (BuildParametersPanel panel in ToolTabs.TabPages.OfType<TabPage>().SelectMany(x => x.Controls.OfType<BuildParametersPanel>()))
            {
                args.Add(panel.Tool.Name, panel.Arguments);
            }

            List<string> shouldRun = new List<string>{ "Shared" };
            foreach (CheckBox step in pnlSteps.Controls.OfType<CheckBox>())
            {
                if (step.Checked && step.Tag is CompileTool t) shouldRun.Add(t.Name);
            }

            profile.Arguments.Clear();

            foreach (KeyValuePair<string, string> kv in args)
            {
                if (shouldRun.Contains(kv.Key))
                {
                    profile.Arguments[kv.Key] = kv.Value;
                }
            }
        }

        private void SwitchToAdvanced(object sender, EventArgs e)
        {
            if (AdvancedPanel.Dock == DockStyle.Fill) return;
            SimplePanel.Dock = DockStyle.None;
            AdvancedPanel.Dock = DockStyle.Fill;
            _simpleSize = Size;
            Size = _advancedSize;
        }

        private void SwitchToSimple(object sender, EventArgs e)
        {
            if (SimplePanel.Dock == DockStyle.Fill) return;
            AdvancedPanel.Dock = DockStyle.None;
            SimplePanel.Dock = DockStyle.Fill;
            _advancedSize = Size;
            Size = _simpleSize;
        }

        private class ProfileWrapper
        {
            public BuildProfile Profile { get; set; }
            public CompilePreset Preset { get; set; }

            public ProfileWrapper(BuildProfile profile)
            {
                Profile = profile;
            }

            public ProfileWrapper(CompilePreset preset)
            {
                Preset = preset;
            }

            public string GetArguments(string name)
            {
                if (Profile != null && Profile.Arguments.ContainsKey(name)) return Profile.Arguments[name];
                if (Preset != null) return Preset.GetArguments(name);
                return "";
            }

            public string GetName()
            {
                return Profile?.Name ?? Preset?.Name ?? "";
            }

            public override string ToString()
            {
                if (Profile != null) return Profile.Name;
                if (Preset != null) return "Preset: " + Preset.Name;
                return "--";
            }
        }
    }
}
