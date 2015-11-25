using Hansoft.ObjectWrapper;
using Hansoft.ObjectWrapper.CustomColumnValues;
using HPMSdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Hansoft.Jean.Behavior.DependencyBehavior
{
    public class DependencyBehavior : AbstractBehavior
    {
        private const string TEAM_PROJECT_PREFIX = "Team - ";
        private static readonly System.Drawing.Color HANSOFT_RED = System.Drawing.Color.FromArgb(0xDC, 0x64, 0x64);

        string title = "DependencyBehavior";
        bool initializationOK = false;
        string projectName;
        List<Project> projects;
        bool inverted = false;

        public DependencyBehavior(XmlElement configuration)
            : base(configuration)
        {
            projectName = GetParameter("HansoftProject");
            string invert = GetParameter("InvertedMatch");
            if (invert != null)
                inverted = invert.ToLower().Equals("yes");
        }

        private void InitializeProjects()
        {
            projects = HPMUtilities.FindProjects(projectName, inverted);
        }

        public override void Initialize()
        {
            initializationOK = false;
            
            InitializeProjects();

            initializationOK = true;
            DoUpdate();
        }
    
        public override string Title
        {
            get { return title; }
        }

        private static bool IsTeamProject(Task task)
        {
            return task.Project.Name.StartsWith(TEAM_PROJECT_PREFIX);
        }

        private static string TeamName(Task task)
        {
            return task.Project.Name.Substring(TEAM_PROJECT_PREFIX.Length);
        }

        private static string CalcAggregatedStatus(IEnumerable<HansoftEnumValue> values)
        {
            if (values.Count() == 0)
                return "Not done";
            if (values.Any(i => (EHPMTaskStatus)i.Value == EHPMTaskStatus.Blocked))
                return "Blocked";
            if (values.All(i => (EHPMTaskStatus)i.Value == EHPMTaskStatus.Completed))
                return "Completed";
            if (values.All(i => (EHPMTaskStatus)i.Value == EHPMTaskStatus.NotDone || (EHPMTaskStatus)i.Value == EHPMTaskStatus.NoStatus))
                return "Not done";
            return "In progress";
        }

        private static string CalcAggregatedStatus(IEnumerable<Task> tasks)
        {
            return CalcAggregatedStatus(tasks.Select(t => t.AggregatedStatus));
        }

        private static string MaxPlannedSprint(CustomColumnValue plannedSprint)
        {
            return plannedSprint.ToString().Split(';').Where(s => s.StartsWith("S")).Max();
        }

        private static bool LeafCompleted(Task t)
        {
            return t.IsCompleted || (EHPMTaskStatus)t.AggregatedStatus.Value == EHPMTaskStatus.Completed;
        }

        public static void InternalDependencies(Release milestone)
        {
            var program = milestone.Project.Name;
            var dependencies = milestone.LinkedTasks
                .Where(t => ProgramTeamsConfig.IsTeamInProgram(program, TeamName(t)) && !LeafCompleted(t))
                .Select(t => new { Name = TeamName(t), PlannedSprint = MaxPlannedSprint(t.GetCustomColumnValue("Planned sprint")) })
                .GroupBy(t => t.Name)
                .Select(t => new { Name = t.Key, MaxPlannedSprint = t.Max(s => s.PlannedSprint) });

            var internalDependencies = dependencies.Count() <= 1 ? "" : dependencies
                .OrderBy(t => t.MaxPlannedSprint)
                .Aggregate(new StringBuilder(), (sb, t) => sb.Append(string.Format("{0} ({1})", t.Name, string.IsNullOrEmpty(t.MaxPlannedSprint) ? "not set" : t.MaxPlannedSprint))
                    .Append(" "), sb => sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : "");
            foreach (Task feature in milestone.ProductBacklogItems)
                feature.SetCustomColumnValue("Internal dependencies", internalDependencies);
        }

        public static void ExternalDependencies(Release milestone)
        {
            var program = milestone.Project.Name;
            var allDependencies = new HashSet<string>();
            var dependencies = milestone.LinkedTasks
                .Where(t => !ProgramTeamsConfig.IsTeamInProgram(program, TeamName(t)))
                .GroupBy(t => TeamName(t), t => t.AggregatedStatus, (key, s) => new { TeamName = key, AggregatedStatuses = s });

            if (dependencies.Count() > 0)
            {
                var col = milestone.Color;
                milestone.Color = HANSOFT_RED;
                foreach (var dependency in dependencies)
                    allDependencies.Add(string.Format("{0} ({1})", dependency.TeamName, CalcAggregatedStatus(dependency.AggregatedStatuses)));
            }

            var externalDependencies = allDependencies.Count() == 0 ? "" : allDependencies.OrderBy(t => t).Aggregate((current, next) => current + ", " + next);
            foreach (Task feature in milestone.ProductBacklogItems)
                feature.SetCustomColumnValue("External dependencies", externalDependencies);
        }

        private void DoUpdate()
        {
            if (initializationOK)
            {
                foreach (Project project in projects)
                {
                    foreach (var milestone in project.ScheduledItems.Where(r => r is Release).Cast<Release>())
                    {
                        var linkedItems = milestone.LinkedTasks;
                        if (linkedItems.Any(t => !ProgramTeamsConfig.IsTeamInProgram(project.Name, TeamName(t))))
                            ExternalDependencies(milestone);
                        if (linkedItems.Any(t => ProgramTeamsConfig.IsTeamInProgram(project.Name, TeamName(t))))
                            InternalDependencies(milestone);
                    }
                }
            }
        }

        public override void OnTaskChangeCustomColumnData(TaskChangeCustomColumnDataEventArgs e)
        {
            if (initializationOK)
            {
                Task task = Task.GetTask(e.Data.m_TaskID);
                if (task is Release && projects.Contains(task.Project))
                {
                    var program = task.Project.Name;
                    if (task.LinkedTasks.Any(t => !ProgramTeamsConfig.IsTeamInProgram(program, TeamName(t))))
                        ExternalDependencies(task as Release);
                    if (task.LinkedTasks.Any(t => ProgramTeamsConfig.IsTeamInProgram(program, TeamName(t))))
                        InternalDependencies(task as Release);
                }
            }
        }
    }
}
