using Microsoft.AspNetCore.Identity;
using Operum.Model.Constants;
using Operum.Model.Models;

namespace Operum.Service.Services.Authentication
{
    /// Length is enforced by Identity's RequiredLength; this adds what it can't express.
    /// Only passwords at or above PasswordPolicy.MinLength belong in the common list.
    public class PasswordPolicyValidator : IPasswordValidator<User>
    {
        private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            "0123456789", "1234567890", "1234567891", "12345678910", "123456789012", "1234567890123",
            "12345678901", "0987654321", "9876543210", "1122334455", "1212121212", "1231231231",
            "123123123123", "123456789a", "123456789q", "12345678ab", "1234abcd5678",
            "1q2w3e4r5t", "1q2w3e4r5t6y", "1qaz2wsx3edc", "1qazxsw23edc", "q1w2e3r4t5", "q1w2e3r4t5y6",
            "qazwsxedcrfv", "qazwsxedc123", "zaq12wsxcde3", "zaq1zaq1zaq1",
            "qwertyuiop", "qwertyuiop1", "qwertyuiop123", "qwertyuiopasdfghjkl", "qwerty1234",
            "qwerty12345", "qwerty123456", "qwertyui12", "qwertyuiop[]", "asdfghjkl1", "asdfghjkl123",
            "asdfghjklq", "asdfghjkl;'", "zxcvbnm123", "zxcvbnmasdfghjkl", "asdfasdfasdf",
            "abcdefghij", "abcdefghijk", "abcdefghijkl", "abcd123456", "abcd1234567", "abc1234567",
            "abc123456789", "abcabcabcabc",
            "password01", "password10", "password11", "password12", "password123", "password1234",
            "password12345", "password123456", "password1!", "password!23", "passw0rd123", "passw0rd12345",
            "password000", "passwordpassword", "mypassword", "mypassword1", "mypassword12", "mypassword123",
            "yourpassword", "newpassword1", "newpassword123", "letmein1234", "letmein12345",
            "welcome123", "welcome1234", "welcome12345", "welcome0123", "administrator", "administrator1",
            "admin12345", "admin123456", "adminadmin", "adminadmin1", "adminadmin123",
            "changeme123", "changemenow", "changeme1234", "iloveyou123", "iloveyou1234", "iloveyou12345",
            "iloveyou2000", "football123", "baseball123", "basketball1", "superman123", "batman12345",
            "trustno1234", "sunshine123", "princess123", "princess1234", "monkey12345", "dragon12345",
            "starwars123", "whatever123", "internet123", "computer123", "michael1234", "jennifer123",
            "1111111111", "2222222222", "3333333333", "4444444444", "5555555555", "6666666666",
            "7777777777", "8888888888", "9999999999", "0000000000", "1234512345", "1234567812345678",
            "aaaaaaaaaa", "aaaaaaaaaaaa", "qqqqqqqqqq", "zzzzzzzzzz",
            "operum1234", "operum12345", "operumoperum", "trackertracker",
        };

        public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string? password)
        {
            if (string.IsNullOrEmpty(password))
                return Task.FromResult(IdentityResult.Success);

            var errors = new List<IdentityError>();

            if (password.Length > PasswordPolicy.MaxLength)
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordTooLong",
                    Description = $"Password must be at most {PasswordPolicy.MaxLength} characters long."
                });
            }

            if (CommonPasswords.Contains(password))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordTooCommon",
                    Description = "This password is too common. Choose a less predictable one."
                });
            }

            if (MatchesAccountDetails(user, password))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordMatchesAccount",
                    Description = "Password cannot be the same as your username or email."
                });
            }

            return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. errors]));
        }

        private static bool MatchesAccountDetails(User user, string password)
        {
            var candidates = new List<string?> { user.UserName, user.Email };
            if (user.Email is { } email)
                candidates.Add(email.Split('@')[0]);

            return candidates.Any(c => !string.IsNullOrEmpty(c)
                && string.Equals(c, password, StringComparison.OrdinalIgnoreCase));
        }
    }
}
