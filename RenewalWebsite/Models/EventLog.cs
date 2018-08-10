using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class EventLog
    {
        public int Id { get; set; }
        public int? EventId { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public DateTime? CreatedTime { get; set; }

        [NotMapped]
        public string StackTrace { get; set; }

        [NotMapped]
        public string Source { get; set; }

        public string EmailId { get; set; }
    }
}
