namespace Operum.Model.Constants
{
    public static class PasswordPolicy
    {
        public const int MinLength = 10;
        // Caps the work a single password hash can cost.
        public const int MaxLength = 128;
    }
}
