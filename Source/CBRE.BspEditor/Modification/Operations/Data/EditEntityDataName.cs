﻿using System.Threading.Tasks;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Primitives.MapObjectData;
using CBRE.BspEditor.Primitives.MapObjects;

namespace CBRE.BspEditor.Modification.Operations.Data
{
    public class EditEntityDataName : IOperation
    {
        private readonly long _id;
        private readonly string _newName;
        private string _oldName;
        public bool Trivial => false;

        public EditEntityDataName(long id, string newName)
        {
            _id = id;
            _newName = newName;
        }

        public async Task<Change> Perform(MapDocument document)
        {
            Change ch = new Change(document);

            IMapObject obj = document.Map.Root.FindByID(_id);
            EntityData data = obj?.Data.GetOne<EntityData>();
            if (data != null)
            {
                _oldName = data.Name;
                data.Name = _newName;
                ch.Update(obj);
            }

            return ch;
        }

        public async Task<Change> Reverse(MapDocument document)
        {
            Change ch = new Change(document);

            IMapObject obj = document.Map.Root.FindByID(_id);
            EntityData data = obj?.Data.GetOne<EntityData>();
            if (data != null && _oldName != null)
            {
                data.Name = _oldName;
                ch.Update(obj);
            }

            return ch;
        }
    }
}