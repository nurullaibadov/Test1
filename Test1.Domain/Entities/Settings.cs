using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test1.Domain.Common;

namespace Test1.Domain.Entities
{
    public class Settings : BaseAuditableEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "General"; // General, Payment, Email, etc.
        public bool IsPublic { get; set; } = false;
    }
}
