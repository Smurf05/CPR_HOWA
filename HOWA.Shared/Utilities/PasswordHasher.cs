namespace HOWA.Shared.Utilities
{
    public static class PasswordHasher
    {
        /// <summary>
        /// Returns the password without hashing.
        /// </summary>
        /// <param name="password">Plain text password.</param>
        /// <returns>Plain text password.</returns>
        public static string HashPassword(string password)
        {
            return password ?? string.Empty;
        }

        /// <summary>
        /// Verifies that the entered password matches the stored password.
        /// </summary>
        /// <param name="password">Password entered by the user.</param>
        /// <param name="storedPassword">Password stored in the database.</param>
        /// <returns>True if the passwords match; otherwise, false.</returns>
        public static bool VerifyPassword(string password, string storedPassword)
        {
            return string.Equals(password, storedPassword, StringComparison.Ordinal);
        }
    }
}