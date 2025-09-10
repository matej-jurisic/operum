using Operum.Model.DTOs;
using Operum.Model.Models;

namespace Operum.Model.Extensions
{
    public static class OwnershipExtensions
    {
        public static bool Owns(this ApplicationUserDto user, Tracker tracker)
        {
            return tracker?.OwnerId == user.Id;
        }

        public static bool Owns(this ApplicationUserDto user, Entry entry)
        {
            if (entry.Tracker == null) throw new InvalidDataException("Cannot check ownership of entry without tracker info.");

            return entry.Tracker.OwnerId == user.Id;
        }

        public static bool Owns(this ApplicationUserDto user, Field field)
        {
            if (field.Tracker == null) throw new InvalidDataException("Cannot check ownership of field without tracker info.");

            return field.Tracker.OwnerId == user.Id;
        }
    }
}
