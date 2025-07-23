using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Contract.UserEvents
{
    public record UserRegistered(string UserId, string Email, string FullName);
}
