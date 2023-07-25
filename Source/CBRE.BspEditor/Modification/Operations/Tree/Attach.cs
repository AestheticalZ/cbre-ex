using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Primitives.MapObjects;

namespace CBRE.BspEditor.Modification.Operations.Tree
{
    public class Attach : IOperation
    {
        private long _parentId;
        private List<IMapObject> _objectsToAttach;
        private List<long> _attachedIds;
        public bool Trivial => false;

        public Attach(long parentId, params IMapObject[] objectsToAttach)
        {
            _parentId = parentId;
            _objectsToAttach = objectsToAttach.ToList();
        }

        public Attach(long parentId, IEnumerable<IMapObject> objectsToAttach)
        {
            _parentId = parentId;
            _objectsToAttach = objectsToAttach.ToList();
        }

        public async Task<Change> Perform(MapDocument document)
        {
            Change ch = new Change(document);

            IMapObject par = document.Map.Root.FindByID(_parentId);
            _attachedIds = _objectsToAttach.Select(x => x.ID).ToList();

            foreach (IMapObject o in _objectsToAttach)
            {
                // Add parent
                ch.Add(o);

                // Add all descendants
                ch.AddRange(o.FindAll());

                o.Hierarchy.Parent = par;
            }
            _objectsToAttach = null;

            return ch;
        }

        public async Task<Change> Reverse(MapDocument document)
        {
            Change ch = new Change(document);

            IMapObject par = document.Map.Root.FindByID(_parentId);
            _objectsToAttach = _attachedIds.Select(x => par.FindByID(x)).Where(x => x != null).ToList();

            foreach (IMapObject o in _objectsToAttach)
            {
                // Remove parent
                ch.Remove(o);

                // Remove all descendants
                ch.RemoveRange(o.FindAll());

                o.Hierarchy.Parent = null;
            }
            _attachedIds = null;

            return ch;
        }
    }
}