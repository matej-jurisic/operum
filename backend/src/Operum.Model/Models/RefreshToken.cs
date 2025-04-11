using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Operum.Model.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Token { get; set; } = default!;
        public string UserId { get; set; } = default!;
       
        public virtual ApplicationUser User { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; } = false;
    }
}
