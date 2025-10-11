using Operum.Model.Formatters;

namespace Operum.Model.Constants
{
    public static class Messages
    {
        public static string ItemNotFound(string itemName) => $"{itemName.Capitalize()} not found.";
        public static string MaxNumberReached(string itemName, int amount) => $"Maximum number of {amount} {itemName} reached.";
        public static string CsvMissingFields(int rowIndex, List<string> missing) => $"Row {rowIndex}: Missing required fields: {string.Join(", ", missing)}";
        public static string CsvMaxNumberReached(int current, int imported, int max) => $"Import would exceed maximum entry limit. Current: {current}, Import: {imported}, Max: {max}";
        public static string Invalid(string itemName) => $"Invalid {itemName}";
        public static string NotAllowed(string itemName) => $"{itemName.Capitalize()} is not allowed.";
        public static string Required(string itemName) => $"{itemName.Capitalize()} is required.";

        public const string InvalidLoginAttempt = "Invalid login attempt.";
        public const string EmailAddressNotConfirmed = "Email address has not been confirmed.";
        public const string AccountLockedOut = "Your account is currently locked out.";
        public const string SuccessfulLogin = "Successfully logged in!";
        public const string UsernameTaken = "User with username {0} already exists!";
        public const string EmailTaken = "User with email {0} already exists!";
        public const string ConfirmationMailError = "Error sending confirmation mail.";
        public const string ConfirmationMailSent = "A confirmation mail has been sent to your inbox!";
        public const string InvalidGoogleToken = "Invalid Google token.";
        public const string SomethingWentWrong = "Something went wrong.";
        public const string EmailAlreadyConfirmed = "Email already confirmed.";
        public const string FielIsEmpty = "File is empty.";
        public const string Success = "Success!";
        public const string NoEntriesFound = "No entries found.";
        public const string AlreadyInRole = "User is already in role.";
        public const string AlreadyInTracker = "User is already added to the tracker.";
        public const string NotInTracker = "User is not added to the tracker.";
    }
}
