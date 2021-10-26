﻿using System;
using System.Linq;
using System.Windows.Forms;
using CBRE.DataStructures.GameData;
using CBRE.Editor.Documents;

namespace CBRE.Editor.UI.Sidebar {
    public partial class EntitySidebarPanel : UserControl {
        public EntitySidebarPanel() {
            InitializeComponent();
        }

        public void RefreshEntities(Document doc) {
            if (doc == null) return;

            EntityTypeList.BeginUpdate();

            var selEnt = EntityTypeList.SelectedItem as GameDataObject;
            EntityTypeList.Items.Clear();
            var sel = selEnt == null ? null : selEnt.Name;
            var def = doc.Game.DefaultPointEntity;
            GameDataObject reselect = null, redef = null;
            foreach (var gdo in doc.GameData.Classes.Where(x => x.ClassType == ClassType.Point).OrderBy(x => x.Name.ToLowerInvariant())) {
                EntityTypeList.Items.Add(gdo);
                if (String.Equals(sel, gdo.Name, StringComparison.OrdinalIgnoreCase)) reselect = gdo;
                if (String.Equals(def, gdo.Name, StringComparison.OrdinalIgnoreCase)) redef = gdo;
            }
            if (reselect == null && redef == null) redef = doc.GameData.Classes
                .Where(x => x.ClassType == ClassType.Point)
                .OrderBy(x => x.Name.StartsWith("info_player_start") ? 0 : 1)
                .FirstOrDefault();
            EntityTypeList.SelectedItem = reselect ?? redef;

            EntityTypeList.EndUpdate();
        }

        public void Clear() {
            EntityTypeList.Items.Clear();
        }

        public GameDataObject GetSelectedEntity() {
            return EntityTypeList.SelectedItem as GameDataObject;
        }
    }
}
