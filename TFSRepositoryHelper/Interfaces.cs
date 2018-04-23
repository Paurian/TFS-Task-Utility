using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSRepositoryHelper
{
    public interface IIterationItem
    {
        string Name { get; set; }
        Guid? Id { get; set; }
        DateTime? StartDate { get; set; }
        DateTime? FinishDate { get; set; }
        string Path { get; set; }
        string QueryPath { get; }
        int WorkingDays { get; }
    }

    public interface IBackLogItem
    {
        int ID { get; set; }
        string Title { get; set; }
        string IterationPath { get; set; }
        string AssignedTo { get; set; }
        string BackLogItemState { get; set; }
        int EstimatedEffort { get; set; }
        string AreaPath { get; set; }
        string Description { get; set; }
        string AcceptanceCriteria { get; set; }
        List<ITaskItem> Tasks { get; set; }
        // Add List<ILinkItem> ???
    }

    public interface ITaskItem
    {
        int ID { get; set; }
        string Title { get; set; }
        string IterationPath { get; set; }
        string AssignedTo { get; set; }
        string TaskState { get; set; }
        bool Blocked { get; set; }
        int EstimatedEffort { get; set; }
        int RemainingEffort { get; set; }
        int ActualEffort { get; set; }
        string ActivityType { get; set; }
        string Area { get; set; }
        string Description { get; set; }
        string Notes { get; set; }
        // Add List<ILinkItem> ???
    }

    public interface ITFSSettings
    {
        string TfsServer { get; set; }
        string TfsWorkStore { get; set; }
        string TfsProject { get; set; }
        string TfsBaseIterationQueryPath { get; set; }
        string TfsArea { get; set; }
        string TfsTeam { get; set; }
        string TfsIteration { get; set; }
    }

    public interface INewTaskSettings
    {
        string NewTaskTitleTemplate { get; set; }
        string NewTaskActivityType { get; set; }
        string NewTaskEstHours { get; set; }
        string NewTaskAssignedTo { get; set; }
        string NewTaskStoryExceptionFilter { get; set; }
        string NewTaskExceptionFilter { get; set; }
    }
}
