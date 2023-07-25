using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Primitives.MapData;

namespace CBRE.BspEditor.Modification.Operations.Data
{
    public class RemoveMapData : IOperation
    {
        private List<IMapData> _dataToRemove;
        public bool Trivial { get; private set; }

        public RemoveMapData(params IMapData[] dataToRemove) : this(false, dataToRemove)
        {
        }

        public RemoveMapData(IEnumerable<IMapData> dataToRemove) : this(false, dataToRemove)
        {
        }

        public RemoveMapData(bool trivial, params IMapData[] dataToRemove) : this(trivial, dataToRemove.AsEnumerable())
        {
        }

        public RemoveMapData(bool trivial, IEnumerable<IMapData> dataToRemove)
        {
            Trivial = trivial;
            _dataToRemove = dataToRemove.ToList();
        }

        public async Task<Change> Perform(MapDocument document)
        {
            Change ch = new Change(document);
            
            foreach (IMapData d in _dataToRemove)
            {
                document.Map.Data.Remove(d);
                ch.Update(d);
            }

            return ch;
        }

        public async Task<Change> Reverse(MapDocument document)
        {
            Change ch = new Change(document);

            foreach (IMapData d in _dataToRemove)
            {
                document.Map.Data.Add(d);
                ch.Update(d);
            }

            return ch;
        }
    }
}