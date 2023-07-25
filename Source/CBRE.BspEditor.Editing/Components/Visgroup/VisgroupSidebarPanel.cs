﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogicAndTrick.Oy;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Modification;
using CBRE.BspEditor.Modification.Operations;
using CBRE.BspEditor.Modification.Operations.Selection;
using CBRE.BspEditor.Primitives.MapData;
using CBRE.BspEditor.Primitives.MapObjectData;
using CBRE.BspEditor.Primitives.MapObjects;
using CBRE.Common.Shell.Commands;
using CBRE.Common.Shell.Components;
using CBRE.Common.Shell.Context;
using CBRE.Common.Shell.Documents;
using CBRE.Common.Shell.Hooks;
using CBRE.Common.Translations;
using CBRE.Shell;

namespace CBRE.BspEditor.Editing.Components.Visgroup
{
    [AutoTranslate]
    [Export(typeof(ISidebarComponent))]
    [Export(typeof(IInitialiseHook))]
    [OrderHint("G")]
    public partial class VisgroupSidebarPanel : UserControl, ISidebarComponent, IInitialiseHook
    {
        [Import] private ITranslationStringProvider _translation;

        public Task OnInitialise()
        {
            Oy.Subscribe<IDocument>("Document:Activated", DocumentActivated);
            Oy.Subscribe<Change>("MapDocument:Changed", DocumentChanged);
            return Task.FromResult(0);
        }

        public string Title { get; set; } = "Visgroups";
        public object Control => this;

        public string EditButton { set { this.InvokeLater(() => { btnEdit.Text = value; }); } }
        public string SelectButton { set { this.InvokeLater(() => { btnSelect.Text = value; }); } }
        public string ShowAllButton { set { this.InvokeLater(() => { btnShowAll.Text = value; }); } }
        public string NewButton { set { this.InvokeLater(() => { btnNew.Text = value; }); } }
        public string AutoVisgroups { get; set; }
        
        private WeakReference<MapDocument> _activeDocument = new WeakReference<MapDocument>(null);

        public VisgroupSidebarPanel()
        {
            InitializeComponent();
            CreateHandle();
        }

        public bool IsInContext(IContext context)
        {
            return context.TryGet("ActiveDocument", out MapDocument _);
        }
        
        private async Task DocumentActivated(IDocument doc)
        {
            MapDocument md = doc as MapDocument;

            _activeDocument = new WeakReference<MapDocument>(md);
            Update(md);
        }

        private async Task DocumentChanged(Change change)
        {
            if (_activeDocument.TryGetTarget(out MapDocument t) && change.Document == t)
            {
                if (change.HasObjectChanges || IsVisgroupDataChange(change))
                {
                    Update(change.Document);
                }
            }
        }

        private static bool IsVisgroupDataChange(Change change)
        {
            return change.HasDataChanges && change.AffectedData.Any(x => x is AutomaticVisgroup || x is Primitives.MapData.Visgroup);
        }

        private void Update(MapDocument document)
        {
            Task.Factory.StartNew(() =>
            {
                if (document == null)
                {
                    this.InvokeLater(() => VisgroupPanel.Clear());
                }
                else
                {
                    List<VisgroupItem> tree = GetItemHierarchies(document);
                    this.InvokeLater(() => VisgroupPanel.Update(tree));
                }
            });
        }

        private List<VisgroupItem> GetItemHierarchies(MapDocument document)
        {
            List<VisgroupItem> list = new List<VisgroupItem>();

            // add user visgroups
            List<Primitives.MapData.Visgroup> visgroups = document.Map.Data.Get<Primitives.MapData.Visgroup>().ToList();
            foreach (Primitives.MapData.Visgroup v in visgroups)
            {
                list.Add(new VisgroupItem(v.Name)
                {
                    CheckState = GetVisibilityCheckState(v.Objects),
                    Colour = v.Colour,
                    Tag = v
                });
            }

            VisgroupItem auto = new VisgroupItem(AutoVisgroups)
            {
                Disabled = true
            };
            list.Insert(0, auto);

            // add auto visgroups
            List<AutomaticVisgroup> autoVisgroups = document.Map.Data.Get<AutomaticVisgroup>().ToList();
            Dictionary<string, VisgroupItem> parents = new Dictionary<string, VisgroupItem> {{"", auto}};
            foreach (AutomaticVisgroup av in autoVisgroups.OrderBy(x => x.Path.Length))
            {
                VisgroupItem parent = auto;
                if (!parents.ContainsKey(av.Path))
                {
                    List<string> path = new List<string>();
                    foreach (string spl in av.Path.Split('/'))
                    {
                        path.Add(spl);
                        string seg = string.Join("/", path);
                        if (!parents.ContainsKey(seg))
                        {
                            VisgroupItem group = new VisgroupItem(_translation.GetString(spl))
                            {
                                Parent = parent,
                                Disabled = true
                            };
                            list.Add(group);
                            parents[seg] = group;
                        }
                        parent = parents[seg];
                    }
                }
                else
                {
                    parent = parents[av.Path];
                }
                list.Add(new VisgroupItem(_translation.GetString(av.Key))
                {
                    CheckState = GetVisibilityCheckState(av.Objects),
                    Tag = av,
                    Parent = parent
                });
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                VisgroupItem v = list[i];
                if (v.Tag != null) continue;

                List<VisgroupItem> children = list.Where(x => x.Parent == v).ToList();
                if (children.All(x => x.CheckState == CheckState.Checked)) v.CheckState = CheckState.Checked;
                else if (children.All(x => x.CheckState == CheckState.Unchecked)) v.CheckState = CheckState.Unchecked;
                else v.CheckState = CheckState.Indeterminate;
            }

            return list;
        }

        private CheckState GetVisibilityCheckState(IEnumerable<IMapObject> objects)
        {
            IEnumerable<bool> bools = objects.Select(x => x.Data.GetOne<VisgroupHidden>()?.IsHidden ?? false);
            return GetCheckState(bools);
        }

        private CheckState GetCheckState(IEnumerable<bool> bools)
        {
            bool[] a = bools.Distinct().ToArray();
            if (a.Length == 0) return CheckState.Checked;
            if (a.Length == 1) return a[0] ? CheckState.Unchecked : CheckState.Checked;
            return CheckState.Indeterminate;
        }

        private IEnumerable<IMapObject> GetVisgroupObjects(VisgroupItem item)
        {
            if (item?.Tag is Primitives.MapData.Visgroup v) return v.Objects;
            if (item?.Tag is AutomaticVisgroup av) return av.Objects;

            IEnumerable<IMapObject> children = VisgroupPanel.GetAllItems().Where(x => x.Parent == item).SelectMany(GetVisgroupObjects);
            return new HashSet<IMapObject>(children);
        }

        private void SelectButtonClicked(object sender, EventArgs e)
        {
            VisgroupItem sv = VisgroupPanel.SelectedVisgroup;
            if (sv != null && _activeDocument.TryGetTarget(out MapDocument md))
            {
                MapDocumentOperation.Perform(md, new Transaction(new Deselect(md.Selection), new Select(GetVisgroupObjects(sv))));
            }
        }

        private void EditButtonClicked(object sender, EventArgs e)
        {
            Oy.Publish("Command:Run", new CommandMessage("BspEditor:Map:Visgroups"));
        }

        private void ShowAllButtonClicked(object sender, EventArgs e)
        {
            if (_activeDocument.TryGetTarget(out MapDocument md))
            {
                List<IMapObject> objects = md.Map.Root.Find(x => x.Data.GetOne<VisgroupHidden>()?.IsHidden == true, true).ToList();
                if (objects.Any())
                {
                    MapDocumentOperation.Perform(md, new TrivialOperation(
                        d => objects.ToList().ForEach(x => x.Data.Replace(new VisgroupHidden(false))),
                        c => c.AddRange(objects)
                    ));
                }
            }
        }

        private void NewButtonClicked(object sender, EventArgs e)
        {

        }

        private void VisgroupToggled(object sender, VisgroupItem visgroup, CheckState state)
        {
            if (state == CheckState.Indeterminate) return;
            bool visible = state == CheckState.Checked;
            List<IMapObject> objects = GetVisgroupObjects(visgroup).SelectMany(x => x.FindAll()).ToList();
            if (objects.Any() && _activeDocument.TryGetTarget(out MapDocument md))
            {
                MapDocumentOperation.Perform(md, new TrivialOperation(
                    d => objects.ForEach(x => x.Data.Replace(new VisgroupHidden(!visible))),
                    c => c.AddRange(objects)
                ));
            }

        }

        private void VisgroupSelected(object sender, VisgroupItem visgroup)
        {

        }
    }
}
