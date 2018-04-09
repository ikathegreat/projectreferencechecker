using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectReferenceChecker
{
    public class Validator
    {
        private readonly List<Project> projectsList;
        private readonly List<Rule> rulesList;

        public List<Rule> ViolationList { get; set; }
        public List<Rule> WarningList { get; set; }

        public Validator(List<Project> aProjectsList, List<Rule> aRulesList)
        {
            ViolationList = new List<Rule>();
            WarningList = new List<Rule>();
            projectsList = aProjectsList;
            rulesList = aRulesList;
        }

        public void Validate()
        {
            Console.Write("Checking projects for rule violations...");
            var notAllowedRules = rulesList.Where(x => x.RuleType == RuleType.NotAllowed);
            var allowedRules = rulesList.Where(x => x.RuleType == RuleType.Allowed);
            var mustIncludeRules = rulesList.Where(x => x.RuleType == RuleType.MustInclude);
            var codeCannotContainRules = rulesList.Where(x => x.RuleType == RuleType.CodeCannotContain);

            foreach (var notAllowedRule in notAllowedRules)
            {
                var applicableProjectNames = GetMatchingStrings(projectsList.Select(x => x.Name).ToList(), notAllowedRule.ProjectToCheck);
                foreach (var projectName in applicableProjectNames)
                {
                    ValidateNotAllowedReferences(notAllowedRule, projectsList.FirstOrDefault(x => x.Name == projectName));
                }
            }
            foreach (var notAllowedRule in allowedRules)
            {
                var applicableProjectNames = GetMatchingStrings(projectsList.Select(x => x.Name).ToList(), notAllowedRule.ProjectToCheck);
                foreach (var projectName in applicableProjectNames)
                {
                    ValidateAllowedReferences(notAllowedRule, projectsList.FirstOrDefault(x => x.Name == projectName));
                }
            }
            foreach (var notAllowedRule in mustIncludeRules)
            {
                var applicableProjectNames = GetMatchingStrings(projectsList.Select(x => x.Name).ToList(), notAllowedRule.ProjectToCheck);
                foreach (var projectName in applicableProjectNames)
                {
                    ValidateMustIncludeRulesReferences(notAllowedRule, projectsList.FirstOrDefault(x => x.Name == projectName));
                }
            }
            foreach (var codeCannotContainRule in codeCannotContainRules)
            {
                var applicableProjectNames = GetMatchingStrings(projectsList.Select(x => x.Name).ToList(), codeCannotContainRule.ProjectToCheck);
                foreach (var projectName in applicableProjectNames)
                {
                    ValidateCodeCannotContain(codeCannotContainRule, projectsList.FirstOrDefault(x => x.Name == projectName));
                }
            }
            Console.WriteLine("Done.");
        }

        private void ValidateNotAllowedReferences(Rule rule, Project project)
        {
            var matchedReferences = GetMatchingStrings(project.References, rule.Reference);
            foreach (var mr in matchedReferences)
            {
                if (rule.ViolationKind == ViolationKind.Violation)
                    ViolationList.Add(new Rule() { RuleName = rule.RuleName, ProjectToCheck = project.Name, RuleType = RuleType.NotAllowed, Reference = mr });
                else if (rule.ViolationKind == ViolationKind.Warning)
                    WarningList.Add(new Rule() { RuleName = rule.RuleName, ProjectToCheck = project.Name, RuleType = RuleType.NotAllowed, Reference = mr });
            }
        }
        private void ValidateAllowedReferences(Rule rule, Project project)
        {
            var matchedReferences = GetMatchingStrings(project.References, rule.Reference);
            foreach (var mr in matchedReferences)
            {
                if (rule.ViolationKind == ViolationKind.Violation)
                    ViolationList.RemoveAll(x => x.ProjectToCheck == project.Name && x.RuleType == RuleType.NotAllowed && x.Reference == mr);
                else if (rule.ViolationKind == ViolationKind.Warning)
                    WarningList.RemoveAll(x => x.ProjectToCheck == project.Name && x.RuleType == RuleType.NotAllowed && x.Reference == mr);
            }
        }
        private void ValidateMustIncludeRulesReferences(Rule rule, Project project)
        {
            var matchedReferences = GetMatchingStrings(project.References, rule.Reference).ToList();
            if (matchedReferences.Count == 0)
            {

                if (rule.ViolationKind == ViolationKind.Violation)
                    ViolationList.Add(new Rule() { RuleName = rule.RuleName, ProjectToCheck = project.Name, RuleType = RuleType.MustInclude, Reference = rule.Reference });
                else if (rule.ViolationKind == ViolationKind.Warning)
                    WarningList.Add(new Rule() { RuleName = rule.RuleName, ProjectToCheck = project.Name, RuleType = RuleType.MustInclude, Reference = rule.Reference });
            }
        }
        private void ValidateCodeCannotContain(Rule rule, Project project)
        {
            foreach (var codeFile in project.CodeFiles)
            {
                var codeFilePath = Path.Combine(Path.GetDirectoryName(project.FilePath), codeFile);
                if (!File.Exists(codeFilePath))
                    continue;
                var fileLinesList = File.ReadAllLines(codeFilePath).ToList();

                var matchedReferences = GetMatchingStrings(fileLinesList, rule.Reference);
                foreach (var mr in matchedReferences)
                {
                    if (rule.ViolationKind == ViolationKind.Violation)
                        ViolationList.Add(new Rule() { RuleName = rule.RuleName, ProjectToCheck = project.Name, RuleType = RuleType.CodeCannotContain, Reference = rule.Reference });
                    else if (rule.ViolationKind == ViolationKind.Warning)
                        WarningList.Add(new Rule() { RuleName = rule.RuleName, ProjectToCheck = project.Name, RuleType = RuleType.CodeCannotContain, Reference = rule.Reference });
                }

            }
        }

        private static IEnumerable<string> GetMatchingStrings(IEnumerable<string> stringList, string searchPattern)
        {
            var matchingStrings = new List<string>();

            if (searchPattern.StartsWith("*") && searchPattern.EndsWith("*")) //Contains
            {
                matchingStrings.AddRange(stringList.Where(x => x.Contains(searchPattern.Replace("*", string.Empty),
                    StringComparison.InvariantCultureIgnoreCase)));
            }
            else if (searchPattern.StartsWith("*")) //Ends with
            {
                matchingStrings.AddRange(stringList.Where(x => x.EndsWith(searchPattern.Replace("*", string.Empty),
                    StringComparison.InvariantCultureIgnoreCase)));
            }
            else if (searchPattern.EndsWith("*")) //Starts with
            {
                matchingStrings.AddRange(stringList.Where(x => x.StartsWith(searchPattern.Replace("*", string.Empty),
                    StringComparison.InvariantCultureIgnoreCase)));
            }
            else //Is
            {
                matchingStrings.AddRange(stringList.Where(x => string.Equals(x, searchPattern,
                    StringComparison.InvariantCultureIgnoreCase)));
            }

            return matchingStrings;
        }

    }
}
