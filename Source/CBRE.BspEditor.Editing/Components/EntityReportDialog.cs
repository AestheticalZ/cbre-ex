﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogicAndTrick.Oy;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Modification;
using CBRE.BspEditor.Modification.Operations.Selection;
using CBRE.BspEditor.Modification.Operations.Tree;
using CBRE.BspEditor.Primitives.MapObjectData;
using CBRE.BspEditor.Primitives.MapObjects;
using CBRE.Common.Shell.Commands;
using CBRE.Common.Shell.Components;
using CBRE.Common.Shell.Context;
using CBRE.Common.Translations;
using CBRE.Shell;

namespace CBRE.BspEditor.Editing.Components
{
    [Export(typeof(IDialog))]
    [AutoTranslate]
    public partial class EntityReportDialog : Form, IDialog, IManualTranslate
    {
        [Import("Shell", typeof(Form))] private Lazy<Form> _parent;
        [Import] private IContext _context;

        private class ColumnComparer : IComparer
        {
            public int Column { get; set; }
            public SortOrder SortOrder { get; set; }

            public ColumnComparer(int column)
            {
                Column = column;
                SortOrder = SortOrder.Ascending;
            }

            public int Compare(object x, object y)
            {
                ListViewItem i1 = (ListViewItem)x;
                ListViewItem i2 = (ListViewItem)y;
                int compare = string.CompareOrdinal(i1.SubItems[Column].Text, i2.SubItems[Column].Text);
                return SortOrder == SortOrder.Descending ? -compare : compare;
            }
        }

        private readonly ColumnComparer _sorter;

        private List<Subscription> _subscriptions;

        public EntityReportDialog()
        {
            InitializeComponent();

            _sorter = new ColumnComparer(0);
            EntityList.ListViewItemSorter = _sorter;
        }

        public void Translate(ITranslationStringProvider strings)
        {
            CreateHandle();
            string prefix = GetType().FullName;
            this.InvokeLater(() =>
            {
                Text = strings.GetString(prefix, "Title");
                ClassNameHeader.Text = strings.GetString(prefix, "ClassHeader");
                EntityNameHeader.Text = strings.GetString(prefix, "NameHeader");
                GoToButton.Text = strings.GetString(prefix, "GoTo");
                DeleteButton.Text = strings.GetString(prefix, "Delete");
                PropertiesButton.Text = strings.GetString(prefix, "Properties");
                FollowSelection.Text = strings.GetString(prefix, "FollowSelection");
                FilterGroup.Text = strings.GetString(prefix, "Filter");
                TypeAll.Text = strings.GetString(prefix, "ShowAll");
                TypePoint.Text = strings.GetString(prefix, "ShowPoint");
                TypeBrush.Text = strings.GetString(prefix, "ShowBrush");
                IncludeHidden.Text = strings.GetString(prefix, "IncludeHidden");
                FilterByKeyValueLabel.Text = strings.GetString(prefix, "FilterByKeyValue");
                FilterByClassLabel.Text = strings.GetString(prefix, "FilterByClass");
                FilterClassExact.Text = strings.GetString(prefix, "Exact");
                FilterKeyValueExact.Text = strings.GetString(prefix, "Exact");
                ResetFiltersButton.Text = strings.GetString(prefix, "ResetFilters");
                CloseButton.Text = strings.GetString(prefix, "Close");
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Oy.Publish("Context:Remove", new ContextInfo("BspEditor:EntityReport"));
        }

	    protected override void OnMouseEnter(EventArgs e)
		{
            Focus();
            base.OnMouseEnter(e);
        }

        public bool IsInContext(IContext context)
        {
            return context.HasAny("BspEditor:EntityReport");
        }

        public void SetVisible(IContext context, bool visible)
        {
            this.InvokeLater(() =>
            {
                if (visible)
                {
                    if (!Visible) Show(_parent.Value);
                    Subscribe();
                    ResetFilters(null, null);
                }
                else
                {
                    Hide();
                    Unsubscribe();
                }
            });
        }

        private void Subscribe()
        {
            if (_subscriptions != null) return;
            _subscriptions = new List<Subscription>
            {
                Oy.Subscribe<Change>("MapDocument:Changed", DocumentChanged),
                Oy.Subscribe<MapDocument>("Document:Activated", DocumentActivated),
                Oy.Subscribe<MapDocument>("MapDocument:SelectionChanged", SelectionChanged)
            };
        }

        private void Unsubscribe()
        {
            if (_subscriptions == null) return;
            _subscriptions.ForEach(x => x.Dispose());
            _subscriptions = null;
        }

        public async Task DocumentActivated(MapDocument document)
        {
            FiltersChanged(null, null);
        }

        public async Task SelectionChanged(MapDocument document)
        {
            if (!FollowSelection.Checked) return;

            MapDocument doc = _context.Get<MapDocument>("ActiveDocument");
            if (doc == null) return;

            IMapObject selection = doc.Selection.GetSelectedParents().LastOrDefault(x => x is Entity);
            SetSelected(selection);
        }

        private async Task DocumentChanged(Change change)
        {
            if (!change.HasObjectChanges) return;

            if (change.Added.Any(x => x is Entity) || change.Updated.Any(x => x is Entity) || change.Removed.Any(x => x is Entity))
            {
                FiltersChanged(null, null);
            }
        }

        private Entity GetSelected()
        {
            return EntityList.SelectedItems.Count == 0 ? null : (Entity) EntityList.SelectedItems[0].Tag;
        }

        private void SetSelected(IMapObject selection)
        {
            this.InvokeLater(() =>
            {
                if (selection == null) return;

                ListViewItem item = EntityList.Items.OfType<ListViewItem>().FirstOrDefault(x => x.Tag == selection);
                if (item == null) return;

                item.Selected = true;
                EntityList.EnsureVisible(EntityList.Items.IndexOf(item));
            });
        }

        private void FiltersChanged(object sender, EventArgs e)
        {
            this.InvokeLater(() =>
            {
                EntityList.BeginUpdate();
                Entity selected = GetSelected();
                EntityList.ListViewItemSorter = null;
                EntityList.Items.Clear();

                MapDocument doc = _context.Get<MapDocument>("ActiveDocument");
                if (doc != null)
                {
                    ListViewItem[] items = doc.Map.Root
                        .Find(x => x is Entity)
                        .OfType<Entity>()
                        .Where(DoFilters)
                        .Select(GetListItem)
                        .ToArray();
                    EntityList.Items.AddRange(items);

                    EntityList.ListViewItemSorter = _sorter;
                    EntityList.Sort();
                    SetSelected(selected);
                }

                EntityList.EndUpdate();
            });
        }

        private ListViewItem GetListItem(Entity entity)
        {
            KeyValuePair<string, string> targetname = entity.EntityData.Properties.FirstOrDefault(x => x.Key.ToLower() == "targetname");
            return new ListViewItem(new[]
                                        {
                                            entity.EntityData.Name,
                                            targetname.Value ?? ""
                                        }) {Tag = entity};
        }

        private bool DoFilters(Entity ent)
        {
            bool hasChildren = ent.Hierarchy.HasChildren;

            if (hasChildren && TypePoint.Checked) return false;
            if (!hasChildren && TypeBrush.Checked) return false;
            if (!IncludeHidden.Checked)
            {
                if (ent.Data.OfType<IObjectVisibility>().Any(x => x.IsHidden)) return false;
            }

            string classFilter = FilterClass.Text.ToUpperInvariant();
            bool exactClass = FilterClassExact.Checked;
            string keyFilter = FilterKey.Text.ToUpperInvariant();
            string valueFilter = FilterValue.Text.ToUpperInvariant();
            bool exactKeyValue = FilterKeyValueExact.Checked;

            if (!string.IsNullOrWhiteSpace(classFilter))
            {
                string name = (ent.EntityData.Name ?? "").ToUpperInvariant();
                if (exactClass && name != classFilter) return false;
                if (!exactClass && !name.Contains(classFilter)) return false;
            }

            if (!string.IsNullOrWhiteSpace(keyFilter))
            {
                if (ent.EntityData.Properties.All(x => x.Key.ToUpperInvariant() != keyFilter)) return false;
                KeyValuePair<string, string> prop = ent.EntityData.Properties.FirstOrDefault(x => x.Key.ToUpperInvariant() == keyFilter);
                string val = prop.Value.ToUpperInvariant();
                if (exactKeyValue && val != valueFilter) return false;
                if (!exactKeyValue && !val.Contains(valueFilter)) return false;
            }

            return true;
        }

        private void ResetFilters(object sender, EventArgs e)
        {
            TypeAll.Checked = true;
            IncludeHidden.Checked = true;
            FilterKeyValueExact.Checked = false;
            FilterClassExact.Checked = false;
            FilterKey.Text = "";
            FilterValue.Text = "";
            FilterClass.Text = "";
            FiltersChanged(null, null);
        }

        private void SortByColumn(object sender, ColumnClickEventArgs e)
        {
            if (_sorter.Column == e.Column)
            {
                _sorter.SortOrder = _sorter.SortOrder == SortOrder.Descending
                                        ? SortOrder.Ascending
                                        : SortOrder.Descending;
            }
            else
            {
                _sorter.Column = e.Column;
                _sorter.SortOrder = SortOrder.Ascending;
            }
            EntityList.Sort();
            SetSelected(GetSelected()); // Reset the scroll value
        }

        private async Task SelectEntity(Entity sel)
        {
            MapDocument doc = _context.Get<MapDocument>("ActiveDocument");
            if (doc == null) return;

            List<IMapObject> currentSelection = doc.Selection.Except(sel.FindAll()).ToList();
            Transaction tran = new Transaction(
                new Deselect(currentSelection),
                new Select(sel.FindAll())
            );
            await MapDocumentOperation.Perform(doc, tran);
        }

        private void GoToSelectedEntity(object sender, EventArgs e)
        {
            Entity selected = GetSelected();
            if (selected == null) return;
            SelectEntity(selected);
            Oy.Publish("MapDocument:Viewport:Focus2D", selected.BoundingBox);
            Oy.Publish("MapDocument:Viewport:Focus3D", selected.BoundingBox);
        }

        private void DeleteSelectedEntity(object sender, EventArgs e)
        {
            MapDocument doc = _context.Get<MapDocument>("ActiveDocument");
            if (doc == null) return;

            Entity selected = GetSelected();
            if (selected == null) return;
            MapDocumentOperation.Perform(doc, new Detatch(selected.Hierarchy.Parent.ID, selected));
        }

        private void OpenEntityProperties(object sender, EventArgs e)
        {
            Entity selected = GetSelected();
            if (selected == null) return;
            SelectEntity(selected).ContinueWith(_ => Oy.Publish("Command:Run", new CommandMessage("BspEditor:Map:Properties")));
        }

        private void CloseButtonClicked(object sender, EventArgs e)
        {
            Close();
        }
    }
}
