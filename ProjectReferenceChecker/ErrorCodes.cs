using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectReferenceChecker
{
    public enum ErrorCodes
    {
        ViolationsFound = 1,
        UnknownCommandLineExceptionError,
        InvalidSecurityMode,
        ProjectsDirectoryDoesNotExist,
        RuleFileDoesNotExist,
        NoProjectsFound,
        WarningsIncreased,
        ViolationsIncreased
    }
}
