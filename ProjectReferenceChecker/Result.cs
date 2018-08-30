using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectReferenceChecker
{
    public class Result
    {
        public int ViolationsCount { get; set; } = 0;
        public int WarningsCount { get; set; } = 0;
        public List<string> WarningsStringList { get; set; } = new List<string>();
        public List<string> ViolationsStringList { get; set; } = new List<string>();
    }
}
