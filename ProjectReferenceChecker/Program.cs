using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;

namespace ProjectReferenceChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser
            {
                ShowUsageOnEmptyCommandline = true,
                AcceptSlash = true
            };

            //Required
            var projectFilesPath = new ValueArgument<string>(
                    'p', "projectFilesPath",
                    "Sets root search path for project files")
                { Optional = false };
            parser.Arguments.Add(projectFilesPath);

            //Optional
            var ruleFilePath = new ValueArgument<string>(
                    'r', "ruleFilePath",
                    "Sets file path for rule file (uses exe location otherwise)")
                { Optional = true };
            parser.Arguments.Add(ruleFilePath);
            var outputProjectsToSearch = new ValueArgument<bool>(
                    'o', "outputProjectsToSearch",
                    "Output to console the list of projects to search through")
                { Optional = true };
            parser.Arguments.Add(outputProjectsToSearch);

            try
            {
                parser.ParseCommandLine(args);
                //parser.ShowParsedArguments();

                var processor = new Processor
                {
                    ProjectsDirectory = projectFilesPath.Value,
                    RuleFilePath = ruleFilePath.Value,
                    OutputProjectsToSearch = outputProjectsToSearch.Value
                };

                processor.Process();
                Environment.ExitCode = 0; //Success

            }
            catch (CommandLineException e)
            {
                Console.WriteLine("Unknown CommandLineException error: " + e.Message);
                Environment.ExitCode = (int)ErrorCodes.UnknownCommandLineExceptionError;
            }
        }
    }
}
