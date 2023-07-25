﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CBRE.BspEditor.Documents;

namespace CBRE.BspEditor.Modification
{
    public class Transaction : IOperation
    {
        private readonly List<IOperation> _operations;
        public bool Trivial => _operations.All(x => x.Trivial);
        public bool IsEmpty => !_operations.Any();

        public Transaction(params IOperation[] operations) : this(operations.ToList())
        {
        }

        public Transaction(IEnumerable<IOperation> operations)
        {
            _operations = operations.ToList();
        }

        public void Add(IOperation operation)
        {
            _operations.Add(operation);
        }

        public void AddRange(IEnumerable<IOperation> operations)
        {
            _operations.AddRange(operations);
        }

        public async Task<Change> Perform(MapDocument document)
        {
            Change ch = new Change(document);
            foreach (IOperation operation in _operations)
            {
                ch.Merge(await operation.Perform(document));
            }
            return ch;
        }

        public async Task<Change> Reverse(MapDocument document)
        {
            Change ch = new Change(document);
            for (int i = _operations.Count - 1; i >= 0; i--)
            {
                IOperation operation = _operations[i];
                ch.Merge(await operation.Reverse(document));
            }
            return ch;
        }
    }
}
