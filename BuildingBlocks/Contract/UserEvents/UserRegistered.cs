using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Contract.UserEvents
{
    public record UserRegistered(Guid UserId, string Email, string FullName)
    {
        public string? Username { get; set; }
    }
}
