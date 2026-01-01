using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities
{
    public class User : BaseAuditableEntity
    {
        public long RecID { get; set; }
        public string BusinessUnit { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime LastAccessedOn { get; set; }
        public string Status { get; set; } = "1";

        public virtual List<UserSession>? Sessions { get; set; }
    }
    public class UserSession
    {
        public long RecID { get; set; }
        public long UserID { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string DeviceInfo { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Status { get; set; } = "1";
        public DateTime UpdatedOn { get; set; }
        public DateTime CreatedOn { get; set; }

        public virtual User? User { get; set; }
    }
   
}
