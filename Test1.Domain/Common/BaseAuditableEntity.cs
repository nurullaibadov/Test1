using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test1.Domain.Common
{
    public abstract class BaseAuditableEntity : BaseEntity
    {
        public string? CreatedBy { get; set;  } 
        public string? UpdatedBy { get; set;  } 
        public string? DeletedBy { get; set;  }     
    }
}
