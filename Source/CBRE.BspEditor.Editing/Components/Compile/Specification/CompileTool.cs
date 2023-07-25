﻿using System.Collections.Generic;
using System.Linq;
using CBRE.Common.Transport;

namespace CBRE.BspEditor.Editing.Components.Compile.Specification
{
    public class CompileTool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public bool Enabled { get; set; }
        public List<CompileParameter> Parameters { get; private set; }

        public CompileTool()
        {
            Parameters = new List<CompileParameter>();
        }

        public static CompileTool Parse(SerialisedObject gs)
        {
            CompileTool tool = new CompileTool
            {
                Name = gs.Get("Name", ""),
                Description = gs.Get("Description", ""),
                Order = gs.Get("Order", 0),
                Enabled = gs.Get("Enabled", true)
            };
            IEnumerable<SerialisedObject> parameters = gs.Children.Where(x => x.Name == "Parameter");
            tool.Parameters.AddRange(parameters.Select(CompileParameter.Parse));
            return tool;
        }
    }
}