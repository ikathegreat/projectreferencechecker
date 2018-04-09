using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectReferenceChecker
{
    public enum RuleType
    {
        NotAllowed,
        MustInclude,
        Allowed,
        CodeCannotContain
    }
    public enum ViolationKind
    {
        Violation,
        Warning
    }

    public class Rule
    {
        public string RuleName { get; set; }
        public string ProjectToCheck { get; set; }
        public RuleType RuleType { get; set; }
        public string Reference { get; set; }
        public ViolationKind ViolationKind { get; set; }
    }
}
