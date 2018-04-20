using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.TeamFoundation.Client;
using System.Net;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.ProcessConfiguration.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.TeamFoundation.Server;
using System.Security.Principal;
using WindowsCredential = Microsoft.VisualStudio.Services.Common.WindowsCredential;
using System.Xml;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;

namespace AddTaskToBacklogItems
{
    class TFSRepository // Warning CA1001  Implement IDisposable on 'TFSRepository' because it creates members of the following IDisposable types: 'TfsTeamProjectCollection'.
    {
        Settings settings;

        public TFSRepository(Settings settings)
        {
            this.settings = settings;
        }

        // Cache This
        private TfsTeamProjectCollection GetProjects(string tfsServer, string tfsWorkStore)
        {
            UriBuilder tfsPath = new UriBuilder(tfsServer);
            tfsPath.Path = tfsWorkStore;

            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsPath.Uri);
            tpc.Authenticate();

            return tpc;
        }

        public Project GetProject(string tfsServer, string tfsWorkStore, string projectName)
        {
            var tpc = GetProjects(tfsServer, tfsWorkStore);
            var workItemStore = new WorkItemStore(tpc);
            var project = (from Project pr in workItemStore.Projects select pr).Where(p => p.Name == projectName).FirstOrDefault();

            return project;
        }

        // Cache This
        public List<string> GetProjectNames(string tfsServer, string tfsWorkStore)
        {
            // The following code even lists projects that are inaccessible
            //var tpc = GetProjects(tfsServer, tfsWorkStore);
            //var workItemStore = new WorkItemStore(tpc);
            //var projectList = (from Project pr in workItemStore.Projects select pr.Name).ToList();

            var css = GetCommonStructureService(tfsServer, tfsWorkStore);
            var projectList = css.ListAllProjects().Select(p => p.Name).ToList();
            projectList.Sort(); // Order the projects by name

            return projectList;
        }

        // Cache This
        public List<string> GetAreas(string tfsServer, string tfsWorkStore, string tfsProject)
        {
            var workItemStore = GetWorkItemStore(tfsServer, tfsWorkStore);
            var teamProjectIterations = workItemStore.Projects[tfsProject].AreaRootNodes;

            return GetRecursivePaths(tfsProject, teamProjectIterations);
        }

        public List<string> GetTeamNames(TfsTeamProjectCollection tfsTeamProjectCollection, string projectName)
        {
            List<string> result = new List<string>();

            TfsTeamService ts = tfsTeamProjectCollection.GetService<TfsTeamService>();
            var teams = ts.QueryTeams(projectName);
            var teamNames = teams.Select(t => t.Name).ToList();
            teamNames.Sort();
            return teamNames;
        }

        public List<string> GetTeams(string tfsServer, string tfsWorkStore, string tfsProject)
        {
            var tfsTeamProjectCollection = GetProjects(tfsServer, tfsWorkStore);
            return GetTeamNames(tfsTeamProjectCollection, tfsProject);
        }

        public string GetTeamPath(TfsTeamProjectCollection tfsTeamProjectCollection, string projectName, string teamName)
        {
            string teamPath = String.Empty;
            TfsTeamService ts = tfsTeamProjectCollection.GetService<TfsTeamService>();
            var team = ts.QueryTeams(projectName).FirstOrDefault(t => t.Name == teamName);
            if (team != null)
            {
                var teamsConfig = tfsTeamProjectCollection.GetService<TeamSettingsConfigurationService>();
                var teamSettings = teamsConfig.GetTeamConfigurations(new List<Guid> { team.Identity.TeamFoundationId }).FirstOrDefault();
                teamPath = teamSettings.ProjectUri;
            }
            return teamPath;
        }

        public List<IterationItem> GetIterationsFromTeam(string tfsServer, string tfsWorkStore, string projectName, string teamName)
        {
            var tfsTeamProjectCollection = GetProjects(tfsServer, tfsWorkStore);
            var projectInfo = GetProjectInfo(tfsServer, tfsWorkStore, projectName);
            var css = GetCommonStructureService(tfsServer, tfsWorkStore);
            return GetIterationsFromTeam(tfsTeamProjectCollection, css, projectInfo, teamName);
        }

        public List<IterationItem> GetIterationsFromTeam(TfsTeamProjectCollection tfsTeamProjectCollection, ICommonStructureService4 css, ProjectInfo projectInfo, string teamName)
        {
            List<string> iterations = GetIterationPathsFromTeam(tfsTeamProjectCollection, projectInfo.Name, teamName);
            return css.GetIterationsWithDates(projectInfo.Uri).Where(i => iterations.Contains(i.QueryPath)).ToList();
        }

        public List<string> GetIterationPathsFromTeamSortedByDate(string tfsServer, string tfsWorkStore, string projectName, string teamName, bool descending = true)
        {
            List<IterationItem> iterations = GetIterationsFromTeam(tfsServer, tfsWorkStore, projectName, teamName);
            List<string> iterationPaths = new List<string>();
            if (descending)
            {
                iterationPaths = iterations.OrderByDescending(i => i.StartDate).Select(i => i.QueryPath).ToList();
            }
            else
            {
                iterationPaths = iterations.OrderBy(i => i.StartDate).Select(i => i.QueryPath).ToList();
            }

            return iterationPaths;
        }

        // TODO: Cache This per project/team if the interface is too slow.
        public List<string> GetIterationPathsFromTeam(TfsTeamProjectCollection tfsTeamProjectCollection, string projectName, string tfsTeam = "")
        {
            List<string> result = new List<string>();

            TfsTeamService ts = tfsTeamProjectCollection.GetService<TfsTeamService>();
            var team = ts.QueryTeams(projectName).FirstOrDefault(t => t.Name == tfsTeam) ?? ts.QueryTeams(projectName).FirstOrDefault(t => t.Name == String.Empty) ?? ts.QueryTeams(projectName).FirstOrDefault();
            if (team != null)
            {
                var teamsConfig = tfsTeamProjectCollection.GetService<TeamSettingsConfigurationService>();
                var teamsSettings = teamsConfig.GetTeamConfigurations(new List<Guid> { team.Identity.TeamFoundationId });
                foreach (var teamSettings in teamsSettings)
                {
                    result.AddRange(teamSettings.TeamSettings.IterationPaths);
                }
            }
            // The empty node, in case there is an iteration without team assignment (??)
            result.Insert(0, String.Empty);

            return result;
        }

        // TODO: Cache This
        // TODO: Make this work from the team instead of the areaPathFilter
        //public List<string> GetIterationPaths(string tfsServer, string tfsWorkStore, string tfsProject, string areaPathFilter = "")
        //{
        //    var workItemStore = GetWorkItemStore(tfsServer, tfsWorkStore);
        //    var teamProjectIterations = workItemStore.Projects[tfsProject].IterationRootNodes;

        //    var iterationPaths = GetRecursivePaths(tfsProject, teamProjectIterations);

        //    if (!String.IsNullOrEmpty(areaPathFilter))
        //    {
        //        iterationPaths = iterationPaths.Where(ip => ip.StartsWith(areaPathFilter, StringComparison.OrdinalIgnoreCase) || areaPathFilter.StartsWith(ip, StringComparison.OrdinalIgnoreCase)).ToList();
        //    }

        //    return iterationPaths;
        //}

        // Cache This
        public List<string> GetActivities()
        {
            // Unless activities are pulled from a custom field, they seem to be statically assigned and unchangable:
            return new List<string>() { "Deployment", "Design", "Development", "Documentation", "Requirements", "Testing" };
        }

        // Cache This
        public List<IterationItem> GetIterationsWithDates(string projectUrl)
        {
            ICommonStructureService4 css = GetCommonStructureServiceFromSettings();
            return css.GetIterationsWithDates(projectUrl).ToList();
        }

        //public List<ScheduleItem> GetRecursiveIterations(string tfsProject) // , NodeCollection nodeCollection)
        //{
        //    // Overhead to remove and make injectable
        //    var strProjectCollection = "http://cstfs:8080/tfs/Engineering/Centracs/Sustaining";
        //    var projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(strProjectCollection));
        //    var WIS = projCollection.GetService<WorkItemStore>();
        //    var teamProject = WIS.Projects["Centracs/Sustaining"]; // var project = GetProject(...)
        //    // var nodeCollection = teamProject.IterationRootNodes;
        //    ICommonStructureService4 css = GetCommonStructureService();

        //    // Actual Code
        //    var iterationResult = new List<ScheduleItem>();
        //    NodeInfo[] structures = css.ListStructures(teamProject.Uri.ToString());
        //    NodeInfo iterations = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectLifecycle"));
        //    // NodeInfo areas = structures.FirstOrDefault(n => n.StructureType.Equals("?"));

        //    // http://blogs.microsoft.co.il/shair/2009/01/30/tfs-api-part-9-get-areaiteration-programmatically/

        //    if (iterations != null)
        //    {
        //        XmlElement iterationsTree = css.GetNodesXml(new[] { iterations.Uri }, true); // GetNodesXml allows access to attributes
        //        // XmlElement areaTree = css.GetNodesXml(
        //        XmlNodeList nodeList = iterationsTree.ChildNodes;

        //        foreach (XmlNode node in nodeList)
        //        {
        //            var nodePath = tfsProject + "\\" + node.Name;

        //            var idstr = node.Attributes["NodeID"].Value.Remove(0, "vstfs:///Classification/Node/".Length);
        //            var id = new Guid(idstr);
        //            string name = node.Attributes["Name"].Value;
        //            string path = node.Attributes["Path"].Value;
        //            DateTime startDate = DateTime.Parse(node.Attributes["StartDate"].Value, CultureInfo.InvariantCulture);
        //            DateTime finishDate = DateTime.Parse(node.Attributes["FinishDate"].Value, CultureInfo.InvariantCulture);

        //            // Found Iteration with start / end dates

        //            //Console.WriteLine("{0} {1} {2}", name, startDate, finishDate);


        //            iterationResult.Add(new ScheduleItem
        //            {
        //                Name = name,
        //                Start = startDate,
        //                Finish = finishDate,
        //                Id = id,
        //                Path = path
        //            });
        //        }
        //    }
        //    return iterationResult;
        //}

        public List<string> GetRecursivePaths(string tfsProject, NodeCollection nodeCollection)
        {
            List<string> nodePaths = new List<string>();

            foreach (Node node in nodeCollection)
            {
                var path = tfsProject + "\\" + node.Name;
                nodePaths.Add(path);
                AppendRecursivePath(node, nodePaths, path);
            }

            return nodePaths;
        }

        private static void AppendRecursivePath(Node node, ICollection<string> pathCollection, string parentIterationName)
        {
            foreach (Node item in node.ChildNodes)
            {
                var path = parentIterationName + "\\" + item.Name;
                pathCollection.Add(path);
                if (item.HasChildNodes)
                {
                    AppendRecursivePath(item, pathCollection, path);
                }
            }
        }

        public TfsTeamProjectCollection GetTeamProjectCollection(bool useCache = true)
        {
            return GetTeamProjectCollection(settings.TfsServer, settings.TfsWorkStore, useCache);
        }

        TfsTeamProjectCollection _tfsTeamProjectCollection = null;
        public TfsTeamProjectCollection GetTeamProjectCollection(string tfsServer, string tfsWorkStore, bool useCache = true)
        {
            // Return the cached collection if we have it.
            if (_tfsTeamProjectCollection != null && useCache)
            {
                return _tfsTeamProjectCollection;
            }

            UriBuilder tfsPath = new UriBuilder(tfsServer);
            tfsPath.Path = tfsWorkStore;

            // ICredentials networkCredential = new NetworkCredential(username, password, domain);
            // ICredentials networkCredential = System.Net.CredentialCache.DefaultNetworkCredentials;
            // WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();

            WindowsCredential windowsCredential = new WindowsCredential(CredentialCache.DefaultNetworkCredentials);
            VssCredentials loggedInUserCredentials = new VssCredentials(windowsCredential); // System.Net.CredentialCache.DefaultNetworkCredentials;

            // Connect to the server and the store, then cache the collection.
            _tfsTeamProjectCollection = new TfsTeamProjectCollection(tfsPath.Uri, loggedInUserCredentials);

            return _tfsTeamProjectCollection;
        }

        public WorkItemStore GetWorkItemStore()
        {
            return GetWorkItemStore(settings.TfsServer, settings.TfsWorkStore);
        }

        public WorkItemStore GetWorkItemStore(string tfsServer, string tfsWorkStore)
        {
            var teamProjectCollection = GetTeamProjectCollection(tfsServer, tfsWorkStore);
            return teamProjectCollection.GetService<WorkItemStore>();
        }

        public TeamSettingsConfigurationService GetTeamSettingsConfiguration()
        {
            var teamProjectCollection = GetTeamProjectCollection();
            return teamProjectCollection.GetService<TeamSettingsConfigurationService>();
        }

        public ICommonStructureService4 GetCommonStructureServiceFromSettings()
        {
            return GetCommonStructureService(settings.TfsServer, settings.TfsWorkStore);
        }

        public ICommonStructureService4 GetCommonStructureService(string tfsServer, string tfsWorkStore)
        {
            var teamProjectCollection = GetTeamProjectCollection(tfsServer, tfsWorkStore);
            return teamProjectCollection.GetService<ICommonStructureService4>();
        }

        public void CreateTest(WorkItemStore workItemStore)
        {
            Project teamProject = workItemStore.Projects[settings.TfsProject];
            Node areaRootNode = teamProject.AreaRootNodes[settings.TfsArea];

            WorkItemType workItemType = teamProject.WorkItemTypes["User Story"];

            // Create the work item. 
            WorkItem userStory = new WorkItem(workItemType)
            {
                // The title is generally the only required field that doesn’t have a default value. 
                // You must set it, or you can’t save the work item. If you’re working with another
                // type of work item, there may be other fields that you’ll have to set.
                Title = "Recently ordered menu",
                Description =
                    "As a return customer, I want to see items that I've recently ordered.",
            };

            // Save the new user story. 
            userStory.Save();
        }

        private ProjectInfo GetProjectInfoFromSettings(string projectName)
        {
            return GetProjectInfo(settings.TfsServer, settings.TfsWorkStore, projectName);
        }

        private ProjectInfo GetProjectInfo(string tfsServer, string tfsWorkStore, string projectName)
        {
            var css = GetCommonStructureService(tfsServer, tfsWorkStore);
            return css.ListAllProjects().Where(p => p.Name == projectName).FirstOrDefault();
        }

        public IterationItem GetCurrentIteration(string tfsServer, string tfsWorkStore, string projectName, string teamName)
        {
            var collection = GetProjects(tfsServer, tfsWorkStore);
            var css = GetCommonStructureService(tfsServer, tfsWorkStore);
            var project = css.ListAllProjects().Where(p => p.Name == projectName).FirstOrDefault();
            return GetCurrentIteration(collection, css, project, teamName);
        }

        public IterationItem GetNextIteration(string tfsServer, string tfsWorkStore, string projectName, string teamName)
        {
            var collection = GetProjects(tfsServer, tfsWorkStore);
            var css = GetCommonStructureService(tfsServer, tfsWorkStore);
            var project = css.ListAllProjects().Where(p => p.Name == projectName).FirstOrDefault();
            return GetNextIteration(collection, css, project, teamName);
        }

        public IterationItem GetPreviousIteration(string tfsServer, string tfsWorkStore, string projectName, string teamName)
        {
            var collection = GetProjects(tfsServer, tfsWorkStore);
            var css = GetCommonStructureService(tfsServer, tfsWorkStore);
            var project = css.ListAllProjects().Where(p => p.Name == projectName).FirstOrDefault();
            return GetPreviousIteration(collection, css, project, teamName);
        }

        public IterationItem GetCurrentIteration(TfsTeamProjectCollection collection, ICommonStructureService4 css, ProjectInfo project, string baseIterationQueryPath)
        {
            return GetIterationOccurrence(collection, css, project, baseIterationQueryPath, IterationOccurrence.Current);
        }

        public IterationItem GetNextIteration(TfsTeamProjectCollection collection, ICommonStructureService4 css, ProjectInfo project, string baseIterationQueryPath)
        {
            return GetIterationOccurrence(collection, css, project, baseIterationQueryPath, IterationOccurrence.Next);
        }

        public IterationItem GetPreviousIteration(TfsTeamProjectCollection collection, ICommonStructureService4 css, ProjectInfo project, string baseIterationQueryPath)
        {
            return GetIterationOccurrence(collection, css, project, baseIterationQueryPath, IterationOccurrence.Prior);
        }

        private enum IterationOccurrence
        {
            Current,
            Prior,
            Next
        }

        private IterationItem GetIterationOccurrence(TfsTeamProjectCollection tfsTeamProjectCollection, ICommonStructureService4 css, ProjectInfo projectInfo, string teamName, IterationOccurrence iterationOccurrence)
        {
            var iterations = GetIterationsFromTeam(tfsTeamProjectCollection, css, projectInfo, teamName);
            var targetIteration = iterations.Where(i => i.StartDate.HasValue && i.StartDate.Value <= DateTime.Now &&
                                                        i.FinishDate.HasValue && i.FinishDate.Value >= DateTime.Now.Date).FirstOrDefault();

            if (iterationOccurrence != IterationOccurrence.Current)
            {
                // If our offset is greater than 0, use the finish date. Otherwise, use the start date.
                // This allows us to move across iterations, since the purpose of the 
                DateTime offsetDate;
                if (iterationOccurrence == IterationOccurrence.Next)
                {
                    offsetDate = targetIteration.FinishDate.Value.AddDays(4);
                }
                else
                {
                    offsetDate = targetIteration.StartDate.Value.AddDays(-4);
                }
                targetIteration = iterations.Where(i => i.StartDate <= offsetDate && i.FinishDate >= offsetDate).FirstOrDefault();
            }

            if (targetIteration == null)
            {
                // get the latest iteration in the list
                targetIteration = iterations.OrderByDescending(i => i.FinishDate).FirstOrDefault();
            }

            return targetIteration;
        }

        private List<string> GetTeamNames(TfsTeamProjectCollection teamProjectCollection, ProjectInfo project, NodeInfo area)
        {
            List<string> result = new List<string>();

            TfsTeamService ts = teamProjectCollection.GetService<TfsTeamService>();
            var teams = ts.QueryTeams(project.Name);
            var teamsConfig = teamProjectCollection.GetService<TeamSettingsConfigurationService>();
            foreach (TeamFoundationTeam team in teams)
            {
                var teamsSettings = teamsConfig.GetTeamConfigurations(new List<Guid> { team.Identity.TeamFoundationId });
                foreach (var teamSettings in teamsSettings)
                {
                    foreach (var item in teamSettings.TeamSettings.TeamFieldValues)
                    {
                        if (item.Value == area.Path)
                        {
                            result.Add(team.Name);
                        }
                    }
                }
            }

            return result;
        }

        private string GetTeamPath(TfsTeamProjectCollection tfsTeamProjectCollection, ProjectInfo projectInfo, string teamName)
        {
            string teamPath = String.Empty;
            List<string> result = new List<string>();

            TfsTeamService ts = tfsTeamProjectCollection.GetService<TfsTeamService>();
            var team = ts.QueryTeams(projectInfo.Name).Where(t => t.Name == teamName).FirstOrDefault();

            if (team != null)
            {
                var teamConfig = tfsTeamProjectCollection.GetService<TeamSettingsConfigurationService>();
                var teamSettings = teamConfig.GetTeamConfigurations(new List<Guid> { team.Identity.TeamFoundationId });
                // teamPath = teamSettings
            }
            //foreach (TeamFoundationTeam team in teams)
            //{
            //    var teamsSettings = teamsConfig.GetTeamConfigurations(new List<Guid> { team.Identity.TeamFoundationId });
            //    foreach (var teamSettings in teamsSettings)
            //    {
            //        foreach (var item in teamSettings.TeamSettings.TeamFieldValues)
            //        {
            //            result.Add(item.Value);
            //        }
            //    }
            //}

            return teamPath;
        }

        private List<IterationItem> GetIterationsForProjectTeam(TfsTeamProjectCollection collection, ICommonStructureService4 css, ProjectInfo project, string teamName)
        {
            TeamSettingsConfigurationService teamConfigService = collection.GetService<TeamSettingsConfigurationService>();
            NodeInfo[] structures = css.ListStructures(project.Uri);
            NodeInfo areas = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectModelHierarchy"));
            NodeInfo iterations = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectLifecycle"));
            XmlElement iterationsTree = css.GetNodesXml(new[] { iterations.Uri }, true);

            var rslt = GetIterationsFromTeam(collection, css, project, teamName);

            string baseIterationQueryPath = GetTeamPath(collection, project, teamName);

            string baseName = (baseIterationQueryPath ?? project.Name) + @"\";
            List<IterationItem> iterationResults = new List<IterationItem>();
            BuildIterationTree(iterationsTree.ChildNodes[0].ChildNodes, baseName, iterationResults);

            if (!String.IsNullOrEmpty(baseIterationQueryPath))
            {
                iterationResults.RemoveAll(i => !i.QueryPath.StartsWith(baseIterationQueryPath));
            }
            return iterationResults;
        }

        // How much of this was migrated / reimplimented in CSS4GetIterationsWithDatesExtension?
        private List<IterationItem> GetIterations(TfsTeamProjectCollection collection, ICommonStructureService4 css, ProjectInfo project, string baseIterationQueryPath)
        {
            TeamSettingsConfigurationService teamConfigService = collection.GetService<TeamSettingsConfigurationService>();
            NodeInfo[] structures = css.ListStructures(project.Uri);
            NodeInfo areas = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectModelHierarchy"));
            NodeInfo iterations = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectLifecycle"));
            XmlElement iterationsTree = css.GetNodesXml(new[] { iterations.Uri }, true);

            var teamName = GetTeamNames(collection, project.Name);
            var iwd = css.GetIterationsWithDates(project.Uri);  // Flat-out Project Node.
            // var gi = css.GetIterationsWithDates(iterations.Uri); // Project Life Cycle Structure Node.
            // Team Node
            var tp = GetTeamPath(collection, project, "Sustaining");
            var tpi = css.GetIterationsWithDates(tp);

            string baseName = (baseIterationQueryPath ?? project.Name) + @"\";
            List<IterationItem> iterationResults = new List<IterationItem>();
            BuildIterationTree(iterationsTree.ChildNodes[0].ChildNodes, baseName, iterationResults);

            if (!String.IsNullOrEmpty(baseIterationQueryPath))
            {
                iterationResults.RemoveAll(i => !i.QueryPath.StartsWith(baseIterationQueryPath));
            }
            return iterationResults;
        }

        private void BuildIterationTree(XmlNodeList items, string baseName, List<IterationItem> iterations)
        {
            foreach (XmlNode node in items)
            {
                if (node.Attributes["NodeID"] != null &&
                    node.Attributes["Name"] != null &&
                    node.Attributes["StartDate"] != null &&
                    node.Attributes["FinishDate"] != null)
                {
                    var idstr = node.Attributes["NodeID"].Value.Remove(0, "vstfs:///Classification/Node/".Length);
                    var id = new Guid(idstr);
                    string name = node.Attributes["Name"].Value;
                    string path = node.Attributes["Path"].Value;
                    DateTime startDate = DateTime.Parse(node.Attributes["StartDate"].Value, CultureInfo.InvariantCulture);
                    DateTime finishDate = DateTime.Parse(node.Attributes["FinishDate"].Value, CultureInfo.InvariantCulture);

                    // Found Iteration with start / end dates

                    //Console.WriteLine("{0} {1} {2}", name, startDate, finishDate);
                    iterations.Add(new IterationItem
                    {
                        Name = name,
                        StartDate = startDate,
                        FinishDate = finishDate,
                        Id = id,
                        Path = path
                    });
                }
                else if (node.Attributes["Name"] != null)
                {
                    string name = node.Attributes["Name"].Value;

                    // Found Iteration without start / end dates
                }

                if (node.ChildNodes.Count > 0)
                {
                    string name = baseName;
                    if (node.Attributes["Name"] != null)
                    {
                        name += node.Attributes["Name"].Value + @"\";
                    }

                    BuildIterationTree(node.ChildNodes, name, iterations);
                }
            }
        }

        //TODO: GetListOfBacklogItemsWithoutAcceptanceCriteria
        //public WorkItemCollection GetListOfBacklogItemsWithoutAcceptanceCriteria(WorkItemStore workItemStore, Settings settings)
        //{
        //}

        //TODO: GetListOfWorkTasksWithoutNotes - returns BLI/BUG name with it.
        //public WorkItemCollection GetListOfTaskItemsWithoutNotes(WorkItemStore workItemStore, Settings settings)
        //{
        //}

        public WorkItemCollection GetListOfBacklogItemsWithoutWork(WorkItemStore workItemStore, Settings settings) // string tfsAreaPath, string tfsIterationPath, string newTaskExceptionFilterValue, string newTaskStoryExceptionFilterValue) settings.TfsArea, settings.TfsIteration, settings.NewTaskExceptionFilter, settings.NewTaskStoryExceptionFilter
        {
            // https://msdn.microsoft.com/en-us/library/bb130306(v=vs.120).aspx
            // https://stackoverflow.com/questions/9266437/tfs-api-how-to-fetch-work-items-from-specific-team-project
            // http://blogs.microsoft.co.il/shair/2010/11/18/tfs-api-part-31-working-with-queries-part-2/

            if (String.IsNullOrEmpty(settings.tfsProject))
            {
                throw new ArgumentException("Project may not be empty or null");
            }

            // Get the area based on the project/team.
            // If that doesn't construct into an area, get just the project's area.
            var tfsArea = String.Format("{0}\\{1}", settings.TfsProject, settings.TfsTeam);
            var projectAreas = GetAreas(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject);
            if (!projectAreas.Contains(tfsArea))
            {
                tfsArea = settings.TfsProject;
            }

            var tfsIteration = settings.TfsIteration;

            if (String.IsNullOrEmpty(settings.TfsIteration))
            {
                tfsIteration = settings.TfsProject;
            }

            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT " +
                                  "[System.Id], [System.Links.LinkType], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [System.Tags] " +
                                "FROM " +
                                  "WorkItemLinks " +
                                "WHERE " +
                                  "[Source].[System.WorkItemType] IN ('Bug', 'Product Backlog Item') AND ");

            if (!String.IsNullOrEmpty(tfsArea))
            {
                queryBuilder.AppendFormat("[Source].[System.AreaPath] UNDER '{0}' AND ", tfsArea); // Either use "=" or "UNDER" for the comparison
            }

            queryBuilder.AppendFormat("[Source].[System.State] <> 'Removed' AND " +
                                      "[Source].[System.IterationPath] = '{0}' AND " +
                                      "[Source].[System.State] <> 'Resolved' AND ", tfsIteration);

            if (!String.IsNullOrEmpty(settings.NewTaskStoryExceptionFilter))
            {
                queryBuilder.AppendFormat("{0} AND ", GetCondition(settings.NewTaskStoryExceptionFilter, "[Source].[System.Title]", "NOT CONTAINS", "AND"));
            }

            queryBuilder.Append("[System.Links.LinkType] <> '' ");

            if (!String.IsNullOrEmpty(settings.NewTaskExceptionFilter))
            {
                queryBuilder.Append("AND " +
                                    "( " +
                                        "[Target].[System.WorkItemType] = 'Task' AND " +
                                        GetCondition(settings.NewTaskExceptionFilter, "[Target].[System.Title]", "CONTAINS", "OR") +
                                    " ) ");
            }

            queryBuilder.Append("ORDER BY [System.Id]");

            if (!String.IsNullOrEmpty(settings.NewTaskExceptionFilter))
            {
                queryBuilder.Append(" mode(DoesNotContain)");
            }

            var q = new Query(workItemStore, queryBuilder.ToString());
            var wili = q.RunLinkQuery();
            var ids = wili.Select(w => w.TargetId).Distinct();
            if (ids.Count() == 0)
            {
                ids = new List<int>() { -1 };
            }
            // ids = new List<int>() { 58356, 58999, 58812, 59000, 58355, 59901, 59904, 59005, 59897, 59003, 58885, 57541, 59696, 59695, 59703, 59704, 59705, 60224, 57583, 59167, 59184, 59183, 60070 };
            string backLogItemWiql = string.Format("Select * FROM WorkItems WHERE [System.Id] in ({0})", string.Join(",", ids));
            q = new Query(workItemStore, backLogItemWiql);
            return q.RunQuery();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conditionalFilter">The text to filter with</param>
        /// <param name="fieldToFilter">E.G. [Source].[System.Title]</param>
        /// <param name="conditionType">CONTAINS or NOT CONTAINS or =</param>
        /// <param name="joinType">AND or OR</param>
        /// <returns></returns>
        private string GetCondition(string conditionalFilter, string fieldToFilter, string conditionType, string joinType)
        {
            StringBuilder conditionQuery = new StringBuilder();
            char[] splitChar = { ',' };
            char stringSentinal = '\'';
            if (conditionalFilter.Contains(stringSentinal))
            {
                stringSentinal = '"';
            }
            string fieldConditionFirst = String.Format("{0} {1} {2}", fieldToFilter, conditionType, stringSentinal);
            string fieldConditionBetween = String.Format("{0} {1} {2}", stringSentinal, joinType, fieldConditionFirst);

            if (conditionalFilter.Contains(','))
            {
                conditionQuery.AppendFormat("({0}{1}{2})",
                    fieldConditionFirst,
                    String.Join(fieldConditionBetween, conditionalFilter.Split(splitChar).Select(s => s.Trim())),
                    stringSentinal);
            }
            else
            {
                conditionQuery.AppendFormat("{0}{1}{2}", fieldConditionFirst, conditionalFilter, stringSentinal);
            }

            return conditionQuery.ToString();
        }

        public List<StoryItem> GetStoryItems(WorkItemCollection workItemCollection)
        {
            List<StoryItem> storyItems = new List<StoryItem>();
            foreach (WorkItem workItem in workItemCollection)
            {
                storyItems.Add(new StoryItem() { ID = workItem.Id, Title = workItem.Title, IsSelected = true });
            }
            return storyItems;
        }

        private int BuildAndSaveSQATask(WorkItemType taskWorkItemType, WorkItem parentWorkItem, Settings settings, WorkItemLinkTypeEnd hierarchyWorkItemLinkTypeEnd)
        {
            // Some items we can assign from the get-go. We must have a task work item, though
            WorkItem sqaTask = new WorkItem(taskWorkItemType)
            {
                AreaId = parentWorkItem.AreaId,
                AreaPath = parentWorkItem.AreaPath,
                IterationPath = parentWorkItem.IterationPath,
                Title = settings.NewTaskTitleTemplate.Replace("{StoryTitle}", parentWorkItem.Title) // Eventually, make all attributes something that can be assigned through a template that includes values from the parent WorkItem.
            };

            SetWorkItemFieldValue(sqaTask, "Activity", settings.NewTaskActivityType);
            SetWorkItemFieldValue(sqaTask, "Estimated Hours", settings.NewTaskEstHours);
            SetWorkItemFieldValue(sqaTask, "Remaining Work", settings.NewTaskEstHours);
            SetWorkItemFieldValue(sqaTask, "Assigned To", settings.NewTaskAssignedTo);

            // Make this item a child of the parentWorkItem:
            sqaTask.Links.Add(new RelatedLink(hierarchyWorkItemLinkTypeEnd, parentWorkItem.Id));

            try
            {
                if (sqaTask.IsValid() == false)
                {
                    Console.WriteLine("The work item is not valid.");
                }
                else
                {
                    // Uncomment this line when we want to actually run this product!
                    sqaTask.Save();
                }
            }
            catch (ValidationException exception)
            {
                Console.WriteLine("The work item threw a validation exception.");
                Console.WriteLine(exception.Message);
                throw;
            }

            // Saving the work item produces an ID that we can return.
            return sqaTask.Id;
        }

        private void SetWorkItemFieldValue(WorkItem workItem, string field, string value)
        {
            // Some items are only reachable through fields. Check to make sure the field applies and the value is allowed before assigning it.
            if (!String.IsNullOrEmpty(value) && workItem.Fields.Contains(field) && (workItem.Fields[field].AllowedValues.Count == 0 || workItem.Fields[field].AllowedValues.Contains(value)))
            {
                workItem.Fields[field].Value = value;
            }

            // Display the results of this change. 
            // if (workItem.IsDirty)
            //     Console.WriteLine("The work item has changed but has not been saved.");

            if (workItem.Fields[field].IsValid == false)
            {
                Console.WriteLine(String.Format("The assigned value ({0}) of the '{1}' field is not valid.", value, field));
            }
        }

        public List<int> AddTaskToEachWorkItemInCollection(WorkItemStore workItemStore, WorkItemCollection workItemCollection, Settings settings, Delegate buildTaskMethod, List<StoryItem> tasks)
        {
            Project teamProject = workItemStore.Projects[settings.TfsProject];
            WorkItemType taskWorkItemType = teamProject.WorkItemTypes["Task"];
            WorkItemLinkType hierarchyWorkItemLinkType = workItemStore.WorkItemLinkTypes["System.LinkTypes.Hierarchy"];
            WorkItemLinkTypeEnd hierarchyWorkItemLinkTypeEnd = workItemStore.WorkItemLinkTypes.LinkTypeEnds[hierarchyWorkItemLinkType.ReverseEnd.Name];
            List<int> newTaskIds = new List<int>();

            foreach (WorkItem workItem in workItemCollection)
            {
                // Only create tasks if they're selected from our user.
                if (tasks.Any(t => t.ID == workItem.Id && t.IsSelected))
                {
                    int taskId = (int)buildTaskMethod.DynamicInvoke(taskWorkItemType, workItem, settings, hierarchyWorkItemLinkTypeEnd);
                    newTaskIds.Add(taskId);
                }
            }

            return newTaskIds;
        }

        public List<int> AddSQATaskToEachWorkItemInCollection(WorkItemStore workItemStore, WorkItemCollection workItemCollection, Settings settings, List<StoryItem> tasks)
        {
            var result = AddTaskToEachWorkItemInCollection(workItemStore, workItemCollection, settings, new Func<WorkItemType, WorkItem, Settings, WorkItemLinkTypeEnd, int>(BuildAndSaveSQATask), tasks);
            settings.IsVerified = false;
            return result;
        }

        private string GetNodeID(string xml)
        {
            var first = "NodeID=\"";
            var start = xml.IndexOf(first) + first.Length;
            var end = xml.IndexOf("\"", start);
            return xml.Substring(start, (end - start));
        }

        // Doesn't Work - Need to determine how to scan through a project's areas to get iterations.
        // otherwise the areas themselves are thought of as iterations and don't reflect our needs.
        public void GetCurrentIterationPath(WorkItemStore workItemStore)
        {
            // Iterations should have dates associated with them
            var irn = workItemStore.Projects[settings.TfsProject].IterationRootNodes;
            foreach (Node inode in irn)
            {
                // Does this supply me with my iteration info?
                var aName = inode.Name;
                var x = inode.Path;
                var u = inode.Uri;
            }

            var teamProjectArea = workItemStore.Projects[settings.TfsProject].AreaRootNodes[settings.TfsArea];
            var css = GetCommonStructureServiceFromSettings();
            var structures = css.ListStructures(teamProjectArea.Uri.ToString());
            var iterations = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectLifecycle"));
            var iterationsTree = css.GetNodesXml(new[] { iterations.Uri }, true);
            foreach (XmlNode node in iterationsTree.ChildNodes)
            {
                var nodeId = GetNodeID(node.OuterXml);
                var nodeInfo = css.GetNode(nodeId);
                var iName = nodeInfo.Name;
                var iStart = nodeInfo.StartDate;
                var iFinish = nodeInfo.FinishDate;
            }
        }

        public List<UserResource> GetContributors(string tfsServer, string tfsWorkStore, string tfsProject)
        {
            return GetGroupAreaMembers(tfsServer, tfsWorkStore, tfsProject, "Contributors", MembershipQuery.Direct);
        }

        public List<UserResource> GetGroupAreaMembers(string tfsServer, string tfsWorkStore, string tfsProject, string userGroupArea)
        {
            List<UserResource> result = null;
            if (userGroupArea != null)
            {
                result = GetGroupAreaMembers(tfsServer, tfsWorkStore, tfsProject, userGroupArea, MembershipQuery.Direct);
            }
            if (result == null || result.Count == 0)
            {
                result = GetGroupAreaMembers(tfsServer, tfsWorkStore, tfsProject, tfsProject, MembershipQuery.Direct);
            }

            if (result == null)
            {
                result = new List<UserResource>();
            }

            return result;
        }

        // Cache This
        public List<UserResource> GetGroupAreaMembers(string tfsServer, string tfsWorkStore, string tfsProject, string userGroupArea, MembershipQuery membershipQueryType)
        {
            var groupList = GetGroups(tfsServer, tfsWorkStore, tfsProject);
            var group = groupList?.FirstOrDefault(o => o.DisplayName.Contains(userGroupArea));

            if (group == null)
            {
                return null;
            }

            var userList = GetGroupMembers(tfsServer, tfsWorkStore, tfsProject, group, membershipQueryType);
            if (userList == null)
            {
                return null;
            }

            return userList.Select(t => new UserResource() { DisplayName = t.DisplayName, UniqueName = t.UniqueName, UniqueUserId = t.UniqueUserId }).ToList();
        }

        public List<string> GetGroupNames(string tfsServer, string tfsWorkStore, string tfsProject)
        {
            var groups = GetGroups(tfsServer, tfsWorkStore, tfsProject);
            return groups.Select(g => g.DisplayName).ToList();
        }

        private TeamFoundationIdentity[] GetGroups(string tfsServer, string tfsWorkStore, string tfsProject)
        {
            ICommonStructureService css = GetCommonStructureService(tfsServer, tfsWorkStore);
            TfsTeamProjectCollection projectCollection = GetProjects(tfsServer, tfsWorkStore);

            IIdentityManagementService ims = projectCollection.GetService<IIdentityManagementService>();

            // get the tfs project INFO
            var projectList = css.ListAllProjects();
            var project = projectList.FirstOrDefault(o => o.Name.Contains(tfsProject));

            // project doesn't exist
            if (project == null) return null;

            // get the tfs group
            TeamFoundationIdentity[] groupList = ims.ListApplicationGroups(project.Uri.ToString(), Microsoft.TeamFoundation.Framework.Common.ReadIdentityOptions.ExtendedProperties);

            return groupList;
        }

        private TeamFoundationIdentity[] GetGroupMembers(string tfsServer, string tfsWorkStore, string tfsProject, TeamFoundationIdentity group, MembershipQuery membershipQueryType)
        {
            TfsTeamProjectCollection projectCollection = GetProjects(tfsServer, tfsWorkStore);
            IIdentityManagementService ims = projectCollection.GetService<IIdentityManagementService>();

            // group doesn't exist
            if (group == null) return null;

            TeamFoundationIdentity groupIdentity = ims.ReadIdentity(IdentitySearchFactor.DisplayName, group.DisplayName, membershipQueryType, ReadIdentityOptions.ExtendedProperties);

            // there are no users
            if (groupIdentity.Members.Length == 0) return null;

            // There are users, so grab their IDs and we'll query for their TFS identity info. Sadly, the Members object is an IdentityDescriptor object, which lacks user names and such.
            var groupMemberIdentifiers = groupIdentity.Members; // .Select(m => new Guid(m.Identifier)).ToArray<Guid>();

            // convert to a list
            TeamFoundationIdentity[] tfia = ims.ReadIdentities(groupIdentity.Members, membershipQueryType, ReadIdentityOptions.ExtendedProperties);
            return tfia;

            //// Now flatten that list
            //Dictionary<int, TeamFoundationIdentity> teamFoundationIdentity = new Dictionary<int, TeamFoundationIdentity>();

            //foreach (var tfi in tfia)
            //{
            //    foreach (var i in tfi)
            //    {
            //        if (!teamFoundationIdentity.ContainsKey(i.UniqueUserId))
            //        {
            //            teamFoundationIdentity.Add(i.UniqueUserId, i);
            //        }
            //    }
            //}

            //return teamFoundationIdentity.Values.ToArray<TeamFoundationIdentity>();
        }

        //// TODO: Return a list of work items that had new tasks created beneath them OR return a list of the new items created....
        //public List<int> AddSQATaskToEachWorkItem(WorkItemStore workItemStore, WorkItemCollection workItemCollection)
        //{
        //    Project teamProject = workItemStore.Projects[TfsProject];
        //    WorkItemType workItemType = teamProject.WorkItemTypes["Task"];
        //    WorkItemLinkType hierarchyWorkItemLinkType = workItemStore.WorkItemLinkTypes["System.LinkTypes.Hierarchy"];
        //    WorkItemLinkTypeEnd hierarchyWorkItemLinkTypeEnd = workItemStore.WorkItemLinkTypes.LinkTypeEnds[hierarchyWorkItemLinkType.ReverseEnd.Name];
        //    List<int> newTaskIds = new List<int>();

        //    foreach (WorkItem workItem in workItemCollection)
        //    {
        //        WorkItem sqaTask = new WorkItem(workItemType)
        //        {
        //            AreaId = workItem.AreaId,
        //            AreaPath = workItem.AreaPath,
        //            IterationPath = workItem.IterationPath,
        //            Title = String.Format("SQA: {0}", workItem.Title) // Eventually, make all attributes something that can be assigned through a template that includes values from the parent WorkItem.
        //        };
        //        if (sqaTask.Fields["Activity"].AllowedValues.Contains("Testing"))
        //        {
        //            sqaTask.Fields["Activity"].Value = "Testing";
        //        }
        //        // sqaTask.Fields["Activity"].Value // Microsoft.VSTS.Common.Activity
        //        sqaTask.Links.Add(new RelatedLink(hierarchyWorkItemLinkTypeEnd, workItem.Id));

        //        sqaTask.Save();

        //        newTaskIds.Add(sqaTask.Id);
        //    }

        //    return newTaskIds;
        //}

        //public List<Node> RunQuery(Guid queryGuid, WorkItemStore workItemStore)
        //{
        //    //// get the query
        //    //var queryDef = workItemStore.GetQueryDefinition(queryGuid);
        //    //var query = new Query(workItemStore, queryDef.QueryText, GetParamsDictionary());

        //    //// get the link types
        //    //var linkTypes = new List<WorkItemLinkType>(workItemStore.WorkItemLinkTypes);

        //    //// run the query
        //    //var list = new List<Node>();
        //    //if (queryDef.QueryType == QueryType.List)
        //    //{
        //    //    foreach (WorkItem wi in query.RunQuery())
        //    //    {
        //    //        list.Add(new WorkItemNode() { WorkItem = wi, RelationshipToParent = "" });
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    var workItemLinks = query.RunLinkQuery().ToList();
        //    //    list = WalkLinks(workItemLinks, linkTypes, null);
        //    //}

        //    //return list;
        //}

        public void UpdateTest(WorkItemStore workItemStore)
        {
            // Get a specific work item from the store. (In this case, 
            // get the work item with ID=1.) 
            WorkItem workItem = workItemStore.GetWorkItem(1);

            // Set the value of a field to one that is not valid, and save the old
            // value so that you can restore it later. 
            string oldAssignedTo = (string)workItem.Fields["Assigned to"].Value;
            workItem.Fields["Assigned to"].Value = "Not a valid user";

            // Display the results of this change. 
            if (workItem.IsDirty)
                Console.WriteLine("The work item has changed but has not been saved.");

            if (workItem.IsValid() == false)
                Console.WriteLine("The work item is not valid.");

            if (workItem.Fields["Assigned to"].IsValid == false)
                Console.WriteLine("The value of the Assigned to field is not valid.");

            // Try to save the work item while it is not valid, and catch the exception. 
            if (workItem.IsValid() == false)
            {
                try
                {
                    workItem.Save();
                }
                catch (ValidationException exception)
                {
                    Console.WriteLine("The work item threw a validation exception.");
                    Console.WriteLine(exception.Message);
                }
            }

            //// Set the state to a valid value that is not the old value. 
            //workItem.Fields["Assigned to"].Value = "ValidUser";

            //// If the work item is valid, save the changes. 
            //if (workItem.IsValid())
            //{
            //    workItem.Save();
            //    Console.WriteLine("The work item was saved this time.");
            //}

            //// Restore the original value of the work item's Assigned to field, and save that change. 
            //workItem.Fields["Assigned to"].Value = oldAssignedTo;
            //workItem.Save();
        }
    }

    public class UserResource
    {
        public string DisplayName { get; set; }
        public string UniqueName { get; set; }
        public int UniqueUserId { get; set; }
    }

    public class StoryItem : ObservableObject
    {
        public int ID { get; set; }
        public string Title { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    RaisePropertyChangedEvent("IsSelected");
                }
            }
        }
    }
}
