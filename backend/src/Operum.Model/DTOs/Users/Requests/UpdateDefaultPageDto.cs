namespace Operum.Model.DTOs.Users.Requests
{
    public class UpdateDefaultPageDto
    {
        /// A route path such as "/dashboard" or "/trackers/{id}". Null or empty clears the
        /// preference and the app falls back to the default dashboard.
        public string? DefaultPage { get; set; }
    }
}
