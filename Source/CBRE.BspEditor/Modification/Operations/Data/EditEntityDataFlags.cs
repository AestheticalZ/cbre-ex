﻿using System.Threading.Tasks;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Primitives.MapObjectData;
using CBRE.BspEditor.Primitives.MapObjects;

namespace CBRE.BspEditor.Modification.Operations.Data
{
    public class EditEntityDataFlags : IOperation
    {
        private readonly long _id;
        private readonly int _newFlags;
        private int _oldFlags;
        public bool Trivial => false;

        public EditEntityDataFlags(long id, int newFlags)
        {
            _id = id;
            _newFlags = newFlags;
        }

        public async Task<Change> Perform(MapDocument document)
        {
            Change ch = new Change(document);

            IMapObject obj = document.Map.Root.FindByID(_id);
            EntityData data = obj?.Data.GetOne<EntityData>();
            if (data != null)
            {
                _oldFlags = data.Flags;
                data.Flags = _newFlags;
                ch.Update(obj);
            }

            return ch;
        }

        public async Task<Change> Reverse(MapDocument document)
        {
            Change ch = new Change(document);

            IMapObject obj = document.Map.Root.FindByID(_id);
            EntityData data = obj?.Data.GetOne<EntityData>();
            if (data != null)
            {
                data.Flags = _oldFlags;
                ch.Update(obj);
            }

            return ch;
        }
    }
}