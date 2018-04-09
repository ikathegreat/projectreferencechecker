using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectReferenceChecker
{
    public class Project
    {

        public Project(string aFilePath)
        {
            FilePath = aFilePath;
            References = new List<string>();
            CodeFiles = new List<string>();

            if (string.IsNullOrEmpty(FilePath))
                return;
            if (File.Exists(FilePath))
                Name = Path.GetFileNameWithoutExtension(FilePath);
        }

        public string FilePath { get; set; }
        public string Name { get; set; }

        public List<string> References { get; set; }
        public List<string> CodeFiles { get; set; }
    }
}
