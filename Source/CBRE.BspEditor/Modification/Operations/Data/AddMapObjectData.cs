using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Primitives.MapObjectData;
using CBRE.BspEditor.Primitives.MapObjects;

namespace CBRE.BspEditor.Modification.Operations.Data
{
    public class AddMapObjectData : IOperation
    {
        private long _id;
        private List<IMapObjectData> _dataToAdd;
        public bool Trivial => false;

        public AddMapObjectData(long id, params IMapObjectData[] dataToAdd)
        {
            _id = id;
            _dataToAdd = dataToAdd.ToList();
        }

        public AddMapObjectData(long id, IEnumerable<IMapObjectData> dataToAdd)
        {
            _id = id;
            _dataToAdd = dataToAdd.ToList();
        }

        public async Task<Change> Perform(MapDocument document)
        {
            Change ch = new Change(document);

            IMapObject obj = document.Map.Root.FindByID(_id);
            if (obj != null)
            {
                foreach (IMapObjectData d in _dataToAdd)
                {
                    obj.Data.Add(d);
                }
                obj.DescendantsChanged();
                ch.Update(obj);
            }

            return ch;
        }

        public async Task<Change> Reverse(MapDocument document)
        {
            Change ch = new Change(document);

            IMapObject obj = document.Map.Root.FindByID(_id);
            if (obj != null)
            {
                foreach (IMapObjectData d in _dataToAdd)
                {
                    obj.Data.Remove(d);
                }
                obj.DescendantsChanged();
                ch.Update(obj);
            }

            return ch;
        }
    }
}