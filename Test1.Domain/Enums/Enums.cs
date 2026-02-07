using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test1.Domain.Enums
{
    public enum BookingStatus
    {
       Pending = 0,
       Confirmed = 1,
       InProgress = 2,
       Completed = 3,
       Cancelled = 4,
       Rejeted = 5,
    }
}
