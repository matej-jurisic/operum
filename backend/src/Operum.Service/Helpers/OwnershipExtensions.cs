using Operum.Model.DTOs;
using Operum.Model.Models;

namespace Operum.Service.Helpers
{
    public static class OwnershipExtensions
    {
        public static bool Owns(this ApplicationUserDto user, Tracker tracker)
        {
            return tracker?.OwnerId == user.Id;
        }

        public static bool Owns(this ApplicationUserDto user, Entry entry)
        {
            if (entry.Tracker == null) throw new InvalidDataException("Cannot check ownership without tracker info.");

            return entry.Tracker.OwnerId == user.Id;
        }
    }
}
