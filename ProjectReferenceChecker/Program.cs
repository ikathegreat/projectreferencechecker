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

            /*
             * -p "C:\Users\paul.ikeda\source\Repos\SX_Core" -w
             */

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
            var outputProjectsToSearch = new SwitchArgument(
                    'o', "outputProjectsToSearch",
                    "Output to console the list of projects to search through", false)
            { Optional = true };
            parser.Arguments.Add(outputProjectsToSearch);
            var waitOnCompletion = new SwitchArgument(
                    'w', "waitOnCompletion",
                    "Wait for user input key when done processing before exiting", false)
            { Optional = true };
            parser.Arguments.Add(waitOnCompletion);

            try
            {
                parser.ParseCommandLine(args);
                //parser.ShowParsedArguments();

                var processor = new Processor
                {
                    ProjectsDirectory = projectFilesPath.Value,
                    RuleFilePath = ruleFilePath.Value,
                    OutputProjectsToSearch = outputProjectsToSearch.Value,
                    PauseBeforeExit = waitOnCompletion.Value

                };

                processor.Process();

                Environment.ExitCode = 0; //Success

            }
            catch (CommandLineException e)
            {
                Console.WriteLine("Unknown CommandLineException error: " + e.Message);
                Environment.ExitCode = (int)ErrorCodes.UnknownCommandLineExceptionError;
            }

            if (waitOnCompletion.Value)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}
