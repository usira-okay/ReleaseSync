using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReleaseSync.Application.Models
{
    public class LastRowIndex(int index)
    {
        public int Index { get; private set; } = index;

        public bool IsAdded { get; private set; } = false;

        public void Add(int i = 1)
        {
            Index += i;
            IsAdded = true;
        }
    }
}
