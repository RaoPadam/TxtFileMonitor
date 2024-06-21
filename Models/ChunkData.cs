using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TxtFileMonitor.Models
{
    public class ChunkData
    {
        public string Hash { get; set; }
        public string Content { get; set; }
        public string PreviousContent { get; set; }
        public bool IsChanged { get; set; }
    }
}
