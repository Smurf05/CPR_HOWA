using System.Threading.Tasks;
using HOWA.Domain.Models;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Framework.Authentication
{
    public class AuthService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Validates credentials against the database.
        /// Returns the <see cref="User"/> on success, or <c>null</c> if credentials are invalid.
        /// </summary>
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _unitOfWork.Users.GetByUsernameAsync(username);
            if (user == null)
                return null;

            // Plain-text comparison
            if (string.Equals(user.Password, password, StringComparison.Ordinal))
                return user;

            return null;
        }
    }
}
