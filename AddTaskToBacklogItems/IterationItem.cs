using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddTaskToBacklogItems
{
    public class IterationItem
    {
        public string Name { get; set; }
        public Guid? Id { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        public string Path { get; set; }
        public string QueryPath
        {
            get
            {
                var path = Path;
                if (path.StartsWith("\\"))
                {
                    path = path.Substring(1);
                }

                if (path.Contains("\\Iteration\\"))
                {
                    path = path.Replace("\\Iteration\\", "\\");
                }

                return path;
            }
        }


        public int WorkingDays
        {
            get
            {
                int count = 0;
                if (StartDate.HasValue && FinishDate.HasValue)
                {
                    for (DateTime index = StartDate.Value; index <= FinishDate.Value; index = index.AddDays(1))
                    {
                        if (index.DayOfWeek != DayOfWeek.Sunday && index.DayOfWeek != DayOfWeek.Saturday)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }
    }
}
