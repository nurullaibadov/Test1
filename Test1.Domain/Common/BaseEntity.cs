using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test1.Domain.Common
{
 
    public abstract class BaseEntity
    {
        public Guid Id { get; set;  }       
        public DateTime CreatedAt { get; set;  }    
        public DateTime? UpdatedAt { get; set;  }       
        public bool IsDeleted { get; set;  }    
        public DateTime? DeletedAt { get; set;  }   
    }
}
