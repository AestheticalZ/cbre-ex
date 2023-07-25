using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Primitives.MapObjectData;
using CBRE.BspEditor.Primitives.MapObjects;
using CBRE.Common.Translations;

namespace CBRE.BspEditor.Editing.Problems
{
    [Export(typeof(IProblemCheck))]
    [AutoTranslate]
    public class GameDataNotFound : IProblemCheck
    {
        public string Name { get; set; }
        public string Details { get; set; }
        public Uri Url => null;
        public bool CanFix => false;

        public async Task<List<Problem>> Check(MapDocument document, Predicate<IMapObject> filter)
        {
            DataStructures.GameData.GameData gd = await document.Environment.GetGameData();
            List<Problem> missing = document.Map.Root.FindAll()
                .Where(x => filter(x))
                .SelectMany(x => x.Data.OfType<EntityData>()
                    .Where(ed => gd.GetClass(ed.Name) == null)
                    .Select(ed => new {Object = x, Data = ed}))
                .Select(x => new Problem {Text = x.Data.Name}.Add(x.Object).Add(x.Data))
                .ToList();
            return missing;
        }

        public Task Fix(MapDocument document, Problem problem)
        {
            throw new NotImplementedException();
        }
    }
}