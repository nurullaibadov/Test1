using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Test1.Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set;  }      
        public string LastName { get; set;  }
        public string? ProfileImageUrl { get; set; }        
        public DateTime? DateOfBirth { get; set;  } 
        public string? Address { get; set;  }   
        public string? City { get; set;  }  
        public string? Country { get; set;  }   
        public string? PostalCode { get; set;  }    
        public string? DriverLicenseNumber { get; set;  }
        public DateTime? DriverLicenseExpiryDate { get; set; }  
        public bool IsVerified { get; set; }        
        public DateTime CreatedAt { get; set; }     
        public DateTime? UpdatedAt { get; set;  }   
        public bool IsDeleted { get; set; } 
        public DateTime? DeletedAt { get; set;  }       
        public virtual ICollection<Booking> Bookings { get; set; }      
        public virtual ICollection<Review> Reviews { get; set;  }   
        public virtual ICollection<Payment> Payments { get; set; }  
        public virtual ICollection<Notification> Notifactions { get; set;  }    
        public virtual Driver? Driver { get; set;  }    

    }
}
