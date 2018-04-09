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
        ViolationsFound,
        UnknownCommandLineExceptionError,
        InvalidSecurityMode,
        ProjectsDirectoryDoesNotExist,
        RuleFileDoesNotExist,
        NoProjectsFound
    }
}
