using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ProjectReferenceChecker
{
    public class Processor
    {
        public string ProjectsDirectory { get; set; }
        public bool OutputProjectsToSearch { get; set; }
        public string RuleFilePath { get; set; }
        public string IgnoreFilesFilePath { get; set; }
        public string BranchName { get; set; }
        public string RepositoryName { get; set; }
        public bool PauseBeforeExit { get; set; } = false;
        public bool RunAndCompareMode { get; set; } = false;

        private List<Project> projectsList;
        private List<Rule> rulesList;
        public void Process()
        {
            var stopwatch = Stopwatch.StartNew();
            ValidateSettings();

            Console.Write(string.Format($"Scanning {ProjectsDirectory}..."));
            var csprojFilesList = Directory.EnumerateFiles(ProjectsDirectory,
                "*.csproj", SearchOption.AllDirectories).ToList();
            Console.WriteLine("Done.");
            Console.WriteLine(string.Format($"Found {csprojFilesList.Count} .csproj files."));

            if (csprojFilesList.Count == 0)
            {
                Console.WriteLine(string.Format($"Error: No .csproj files found in {ProjectsDirectory}"));
                Exit(ErrorCodes.NoProjectsFound);
            }

            projectsList = new List<Project>();

            //Get projects and their references
            Console.Write("Reading project references...");
            Parallel.ForEach(csprojFilesList, CreateProjectAndReferences);
            Console.WriteLine("Done.");

            //Exclude any specific files by name
            if (File.Exists(IgnoreFilesFilePath))
            {
                var beforeProjectCount = projectsList.Count();
                var fileLinesList = File.ReadAllLines(IgnoreFilesFilePath).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                fileLinesList.RemoveAll(x => (x.Trim().StartsWith("//") || x.Trim().StartsWith(";")));

                foreach (var ignore in fileLinesList)
                {
                    var trimmedIgnore = ignore.Trim();
                    IgnoreProjects(trimmedIgnore);
                }
                Console.WriteLine(string.Format($"Ignoring {beforeProjectCount - projectsList.Count} project(s) with {Constants.IgnoreFilesFileName}."));
            }

            BuildRules();

            var validator = new Validator(projectsList, rulesList);
            validator.Validate();

            var warningsStringList = new List<string>();
            var violationsStringList = new List<string>();

            //Warnings
            validator.WarningList.Where(x => x.RuleType != RuleType.CodeCannotContain).ToList()
                    .ForEach(x => warningsStringList.Add(string.Format($" {x.RuleName} Warning: \"{x.ProjectToCheck}\" {x.RuleType} reference to \"{x.Reference}\"")));
            validator.WarningList.Where(x => x.RuleType == RuleType.CodeCannotContain).ToList()
                .ForEach(x => warningsStringList.Add(string.Format($" {x.RuleName} Warning: \"{x.ProjectToCheck}\" {x.RuleType} \"{x.Reference}\"")));
            //Violations
            validator.ViolationList.Where(x => x.RuleType != RuleType.CodeCannotContain).ToList()
                .ForEach(x => violationsStringList.Add(string.Format($" {x.RuleName} Violation: \"{x.ProjectToCheck}\" {x.RuleType} reference to \"{x.Reference}\"")));
            validator.ViolationList.Where(x => x.RuleType == RuleType.CodeCannotContain).ToList()
                .ForEach(x => violationsStringList.Add(string.Format($" {x.RuleName} Violation: \"{x.ProjectToCheck}\" {x.RuleType} \"{x.Reference}\"")));

            warningsStringList.ForEach(Console.WriteLine);
            violationsStringList.ForEach(Console.WriteLine);

            Console.WriteLine(string.Format($"Found {validator.WarningList.Count} warnings."));
            Console.WriteLine(string.Format($"Found {validator.ViolationList.Count} violations."));
            Console.WriteLine("Validation completed.");
            stopwatch.Stop();
            var t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"Elapsed time: {t.Hours:D2}h:{t.Minutes:D2}m:{t.Seconds:D2}s:{t.Milliseconds:D3}ms");

            if (!string.IsNullOrEmpty(RepositoryName) && !string.IsNullOrEmpty(BranchName))
            {
                var resultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SigmaTEK", "Repository Integrity", RepositoryName, BranchName, "References");

                Directory.CreateDirectory(resultDir);
                var resultFile = Path.Combine(resultDir, "Result.xml");

                var result = new Result()
                {
                    ViolationsCount = validator.ViolationList.Count,
                    WarningsCount = validator.WarningList.Count,
                    WarningsStringList = warningsStringList,
                    ViolationsStringList = violationsStringList
                };

                if (RunAndCompareMode)
                {
                    var prevResult = LoadPrevResult(resultFile, new Result());

                    if (prevResult.ViolationsCount < result.ViolationsCount)
                    {
                        Exit(ErrorCodes.ViolationsIncreased);
                    }
                    else if (prevResult.WarningsCount < result.WarningsCount)
                    {
                        Exit(ErrorCodes.WarningsIncreased);
                    }
                }
                else
                {
                    SaveResult(resultFile, result);
                }
            }
            else
            {
                if (RunAndCompareMode)
                    Console.WriteLine($"Error: Run and compare mode enabled, but repository and/or branch names are empty.");
            }


            if (validator.ViolationList.Count > 0)
            {
                Exit(ErrorCodes.ViolationsFound);
            }
        }

        private static Result LoadPrevResult(string resultFile, Result prevResult)
        {
            TextReader reader = new StreamReader(resultFile);
            var xmlSerializer = new XmlSerializer(typeof(Result));

            try
            {
                prevResult = (Result)xmlSerializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured while reading from XML: " + ex.Message);
            }
            finally
            {
                reader.Close();
            }

            return prevResult;
        }

        private static void SaveResult(string resultFile, Result result)
        {
            TextWriter writer = new StreamWriter(resultFile);
            var xmlSerializer = new XmlSerializer(typeof(Result));

            try
            {
                xmlSerializer.Serialize(writer, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured while writing to XML: " + ex.Message);
            }
            finally
            {
                writer.Close();
            }
        }


        private void BuildRules()
        {
            Console.Write("Reading rule file...");
            rulesList = new List<Rule>();
            var ruleLinstList = File.ReadAllLines(RuleFilePath).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            ruleLinstList.RemoveAll(x => (x.Trim().StartsWith("//") || x.Trim().StartsWith(";")));
            foreach (var line in ruleLinstList)
            {
                var parameters = line.Split(',');
                if (parameters.Length < 4)
                    continue;
                try
                {
                    var rule = new Rule()
                    {
                        RuleName = parameters[0],
                        ProjectToCheck = parameters[1],
                        RuleType = (RuleType)Enum.Parse(typeof(RuleType), parameters[2]),
                        Reference = parameters[3],
                        ViolationKind = parameters.ElementAtOrDefault(4) == null ? ViolationKind.Violation
                            : (ViolationKind)Enum.Parse(typeof(ViolationKind), parameters[4])
                    };

                    rulesList.Add(rule);
                }
                catch
                {
                    // ignored
                }
            }
            Console.WriteLine("Done.");
            Console.WriteLine($"{rulesList.Count} rules found.");
        }

        private void IgnoreProjects(string trimmedIgnore)
        {
            //Contains
            if (trimmedIgnore.StartsWith("*") && trimmedIgnore.EndsWith("*"))
            {
                projectsList.RemoveAll(x => x.Name.Contains(trimmedIgnore.Replace("*", string.Empty), StringComparison.InvariantCultureIgnoreCase));
            }
            //Ends with
            else if (trimmedIgnore.StartsWith("*"))
            {
                projectsList.RemoveAll(x => x.Name.EndsWith(trimmedIgnore.Replace("*", string.Empty), StringComparison.InvariantCultureIgnoreCase));
            }
            //Starts with
            else if (trimmedIgnore.EndsWith("*"))
            {
                projectsList.RemoveAll(x => x.Name.StartsWith(trimmedIgnore.Replace("*", string.Empty), StringComparison.InvariantCultureIgnoreCase));
            }
            //Is
            else
            {
                projectsList.RemoveAll(x => string.Equals(x.Name, trimmedIgnore, StringComparison.InvariantCultureIgnoreCase));
            }
        }


        private void ValidateSettings()
        {
            if (!Directory.Exists(ProjectsDirectory))
            {
                Console.WriteLine(string.Format($"Error: Cannot locate rule file at {RuleFilePath}"));
                Exit(ErrorCodes.ProjectsDirectoryDoesNotExist);
            }

            if (string.IsNullOrEmpty(RuleFilePath))
                RuleFilePath = Path.Combine(Environment.CurrentDirectory, Constants.RuleFileName);
            if (string.IsNullOrEmpty(IgnoreFilesFilePath))
                IgnoreFilesFilePath = Path.Combine(Environment.CurrentDirectory, Constants.IgnoreFilesFileName);

            if (!File.Exists(RuleFilePath))
            {
                Console.WriteLine(string.Format($"Error: Cannot locate rule file at {RuleFilePath}"));
                Exit(ErrorCodes.RuleFileDoesNotExist);
            }
        }

        private void CreateProjectAndReferences(string csprojFile)
        {
            var project = new Project(csprojFile);
            var fileLines = File.ReadAllLines(csprojFile);
            foreach (var s in fileLines)
            {
                if (!s.Contains(@"Reference Include=") && !s.Contains(@"Compile Include="))
                    continue;

                var regex = new Regex("\"([^\"]*)\"");
                var match = CleanProjectIncludeString(regex.Match(s).Value);
                if (s.Contains(@"Reference Include="))
                    project.References.Add(match.Contains(',') ? match.Split(',')[0] : match);
                else if (s.Contains(@"Compile Include="))
                    project.CodeFiles.Add(match + ".cs");

            }
            projectsList.Add(project);
        }

        /// <summary>
        /// Given an input string remove possible extra characters from .csproj
        /// reference string line to provide just reference name
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static string CleanProjectIncludeString(string line)
        {
            var replace = line.Replace("\"", string.Empty);
            return Path.GetFileNameWithoutExtension(replace);
        }

        private void Exit(ErrorCodes errorCode)
        {
            if (PauseBeforeExit)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            Environment.Exit((int)errorCode);
        }
    }
}
