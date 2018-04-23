using TFSRepositoryHelper;
using System.Collections.Generic;

namespace AddTaskToBacklogItems
{
    public class StoryItem : ObservableObject, IBackLogItem
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string IterationPath { get; set; }
        public string AssignedTo { get; set; }
        public string BackLogItemState { get; set; }
        public int EstimatedEffort { get; set; }
        public string AreaPath { get; set; }
        public string Description { get; set; }
        public string AcceptanceCriteria { get; set; }
        public List<ITaskItem> Tasks { get; set; }

        // IsSelected with Observable properties is what sets this apart from a TaskItem
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
