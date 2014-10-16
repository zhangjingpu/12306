using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainAssistant.Models
{
    public class QueryOrderWaitTime
    {
        public bool Status { get; set; }
        public int Count { get; set; }
        public int WaitTime { get; set; }
        public string RequestId { get; set; }
        public int WaitCount { get; set; }
        public string TourFlag { get; set; }
        public string OrderId { get; set; }
    }
}
