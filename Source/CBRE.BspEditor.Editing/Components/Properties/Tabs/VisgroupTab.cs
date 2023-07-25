﻿using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogicAndTrick.Oy;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Editing.Components.Visgroup;
using CBRE.BspEditor.Modification;
using CBRE.BspEditor.Modification.Operations.Data;
using CBRE.BspEditor.Primitives.MapObjectData;
using CBRE.BspEditor.Primitives.MapObjects;
using CBRE.Common.Shell.Commands;
using CBRE.Common.Shell.Context;
using CBRE.Common.Translations;
using CBRE.Shell;

namespace CBRE.BspEditor.Editing.Components.Properties.Tabs
{
    /// <summary>
    /// Visgroups are a way to group multiple objects together in multiple different ways.
    /// Visgroups are hierarchical and an object can be a member of any number of visgroups.
    /// Membership of a visgroup implies membership of the visgroup's parent (and so on).
    /// 
    /// Visgroups are always available as long as at least one object is selected.
    /// Automatic visgroups are handled by the editor and are not available to be modified.
    /// </summary>
    [AutoTranslate]
    [Export(typeof(IObjectPropertyEditorTab))]
    public sealed partial class VisgroupTab : UserControl, IObjectPropertyEditorTab
    {
        private Dictionary<VisgroupItem, CheckState> _state;

        /// <inheritdoc />
        public string OrderHint => "Y";

        /// <inheritdoc />
        public Control Control => this;

        public string MemberOfGroup
        {
            get => lblMemberOfGroup.Text;
            set => this.InvokeLater(() => lblMemberOfGroup.Text = value);
        }

        public string EditVisgroups
        {
            get => btnEditVisgroups.Text;
            set => this.InvokeLater(() => btnEditVisgroups.Text = value);
        }

        /// <inheritdoc />
        public bool HasChanges => GetMembershipChanges().Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public VisgroupTab()
        {
            InitializeComponent();
            CreateHandle();

            _state = new Dictionary<VisgroupItem, CheckState>();
        }

        /// <inheritdoc />
        public bool IsInContext(IContext context, List<IMapObject> objects)
        {
            return context.TryGet("ActiveDocument", out MapDocument _)
                && objects.Any(x => !(x is Root));
        }

        /// <inheritdoc />
        public async Task SetObjects(MapDocument document, List<IMapObject> objects)
        {
            visgroupPanel.InvokeLater(() =>
            {
                _state = GetVisgroups(document, objects);
                visgroupPanel.Update(_state.Keys);
            });
        }

        /// <summary>
        /// Get the list of visgroup membership that has been changed since the objects were set.
        /// Indeterminate checkboxes are never a change.
        /// </summary>
        private Dictionary<Primitives.MapData.Visgroup, bool> GetMembershipChanges()
        {
            Dictionary<Primitives.MapData.Visgroup, bool> dic = new Dictionary<Primitives.MapData.Visgroup, bool>();

            foreach (KeyValuePair<VisgroupItem, CheckState> checkState in visgroupPanel.GetAllCheckStates().Where(x => x.Value != CheckState.Indeterminate))
            {
                if (_state.All(x => x.Key.Tag != checkState.Key.Tag)) continue;
                KeyValuePair<VisgroupItem, CheckState> state = _state.First(x => x.Key.Tag == checkState.Key.Tag);
                if (checkState.Value != state.Value) dic[(Primitives.MapData.Visgroup) state.Key.Tag] = checkState.Value == CheckState.Checked;
            }

            return dic;
        }

        /// <summary>
        /// Get the list of visgroups in the document and the membership states of the given objects.
        /// </summary>
        private Dictionary<VisgroupItem, CheckState> GetVisgroups(MapDocument document, List<IMapObject> objects)
        {
            Dictionary<VisgroupItem, CheckState> d = new Dictionary<VisgroupItem, CheckState>();
            if (document == null || objects == null) return d;

            Dictionary<long, int> objGroups = objects
                .SelectMany(x => x.Data.Get<VisgroupID>().Select(v => new {Object = x, Visgroup = v.ID}))
                .GroupBy(x => x.Visgroup)
                .ToDictionary(x => x.Key, x => x.Count());

            foreach (Primitives.MapData.Visgroup visgroup in document.Map.Data.Get<Primitives.MapData.Visgroup>())
            {
                long id = visgroup.ID;
                CheckState cs = CheckState.Unchecked;
                if (objGroups.ContainsKey(id))
                {
                    if (objGroups[id] == objects.Count) cs = CheckState.Checked;
                    else cs = CheckState.Indeterminate;
                }
                d.Add(new VisgroupItem(visgroup.Name)
                {
                    Tag = visgroup,
                    CheckState = cs,
                    Colour = visgroup.Colour
                }, cs);
            }

            return d;
        }

        /// <inheritdoc />
        public IEnumerable<IOperation> GetChanges(MapDocument document, List<IMapObject> objects)
        {
            Dictionary<Primitives.MapData.Visgroup, bool> mc = GetMembershipChanges();
            if (mc.Count == 0) yield break;

            foreach (IMapObject mo in objects)
            {
                Dictionary<long, VisgroupID> visgroups = mo.Data.Get<VisgroupID>().ToDictionary(x => x.ID);
                foreach (KeyValuePair<Primitives.MapData.Visgroup, bool> kv in mc)
                {
                    long id = kv.Key.ID;
                    if (kv.Value && !visgroups.ContainsKey(id))
                    {
                        // Object should be a member of this visgroup but it's not - add it
                        yield return new AddMapObjectData(mo.ID, new VisgroupID(id));
                    }
                    else if (!kv.Value && visgroups.ContainsKey(id))
                    {
                        // Object should not be a member of this visgroup but it is - remove it
                        yield return new RemoveMapObjectData(mo.ID, visgroups[id]);
                    }
                }
            }
        }

        private void VisgroupToggled(object sender, VisgroupItem visgroup, CheckState state)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasChanges)));
        }

        private void EditVisgroupsClicked(object sender, System.EventArgs e)
        {
            Oy.Publish("Command:Run", new CommandMessage("BspEditor:Map:Visgroups"));
        }
    }
}
