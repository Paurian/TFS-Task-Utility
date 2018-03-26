using System;
using System.Linq;
using Microsoft.TeamFoundation.Server;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;

namespace AddTaskToBacklogItems // TfsStatusBoardTools
{
    public static class CSS4GetIterationsWithDatesExtension
    {
        public static IEnumerable<IterationItem> GetIterationsWithDates(this ICommonStructureService4 css, string projectUri)
        {
            NodeInfo[] structures = css.ListStructures(projectUri);
            NodeInfo iterations = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectLifecycle"));
            List<IterationItem> schedule = null;

            if (iterations != null)
            {
                string projectName = css.GetProject(projectUri).Name;

                XmlElement iterationsTree = css.GetNodesXml(new[] { iterations.Uri }, true);
                GetIterationDates(iterationsTree.ChildNodes[0], projectName, ref schedule);
            }

            return schedule;
        }

        //public class ScheduleInfo
        //{
        //    public string Path { get; set; }
        //    public DateTime? StartDate { get; set; }
        //    public DateTime? EndDate { get; set; }
        //}

        private static void GetIterationDates(XmlNode node, string projectName, ref List<IterationItem> schedule)
        {
            if (schedule == null)
            {
                schedule = new List<IterationItem>();
            }

            if (node != null)
            {
                string iterationPath = node.Attributes["Path"].Value;
                if (!string.IsNullOrEmpty(iterationPath))
                {
                    // Attempt to read the start and end dates if they exist.
                    string strStartDate = (node.Attributes["StartDate"] != null) ? node.Attributes["StartDate"].Value : null;
                    string strEndDate = (node.Attributes["FinishDate"] != null) ? node.Attributes["FinishDate"].Value : null;

                    DateTime? nullableStartDate = null, nullableEndDate = null;
                    DateTime startDate, endDate;

                    if (!string.IsNullOrEmpty(strStartDate) && !string.IsNullOrEmpty(strEndDate))
                    {
                        bool datesValid = true;

                        // Use null dates unless both dates are valid.
                        datesValid &= DateTime.TryParse(strStartDate, out startDate);
                        datesValid &= DateTime.TryParse(strEndDate, out endDate);
                        if (datesValid)
                        {
                            nullableStartDate = startDate; // DateTime.Parse(node.Attributes["StartDate"].Value, CultureInfo.InvariantCulture);
                            nullableEndDate = endDate; // DateTime.Parse(node.Attributes["FinishDate"].Value, CultureInfo.InvariantCulture);
                        }
                    }

                    var idstr = node.Attributes["NodeID"].Value.Remove(0, "vstfs:///Classification/Node/".Length);
                    Guid? nullableId = null;

                    if (!string.IsNullOrEmpty(idstr))
                    {
                        Guid id;
                        if (Guid.TryParse(idstr, out id))
                        {
                            nullableId = id;
                        }
                    }

                    var nodePath = projectName + "\\" + node.Name;
                    string name = node.Attributes["Name"].Value;
                    string path = node.Attributes["Path"].Value;

                    schedule.Add(new IterationItem
                    {
                        // Path = iterationPath.Replace(string.Concat("\\", projectName, "\\Iteration"), projectName),
                        Id = nullableId,
                        Name = name,
                        Path = path,
                        StartDate = nullableStartDate,
                        FinishDate = nullableEndDate
                    });
                }

                // Visit any child nodes (sub-iterations).
                if (node.FirstChild != null)
                {
                    // The first child node is the <Children> tag, which we'll skip.
                    for (int nChild = 0; nChild < node.ChildNodes[0].ChildNodes.Count; nChild++)
                    {
                        GetIterationDates(node.ChildNodes[0].ChildNodes[nChild], projectName, ref schedule);
                    }
                }
            }
        }
    }
}
