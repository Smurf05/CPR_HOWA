using System.Data;
using System.Threading.Tasks;
using Dapper;
using HOWA.Domain.Interfaces;
using HOWA.Domain.Models;

namespace HOWA.Repository.Repositories
{
    public class UserRepository : BaseRepository, IUserRepository
    {
        public UserRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<User> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Users WHERE UserId = @Id";
            using (var db = CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
            }
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            const string sql = "SELECT * FROM Users WHERE Username = @Username";
            using (var db = CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
            }
        }

        public async Task<bool> AddAsync(User user)
        {
            const string sql = @"
                INSERT INTO Users (Username, Password, Role, FirstName, LastName, Email, ContactNo, CreatedAt)
                VALUES (@Username, @Password, @Role, @FirstName, @LastName, @Email, @ContactNo, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using (var db = CreateConnection())
            {
                var id = await db.QuerySingleAsync<int>(sql, user);
                if (id > 0)
                {
                    user.UserId = id;
                    return true;
                }
                return false;
            }
        }

        public async Task<bool> UpdateAsync(User user)
        {
            const string sql = @"
                UPDATE Users 
                SET Username = @Username, 
                    Password = @Password, 
                    Role = @Role, 
                    FirstName = @FirstName, 
                    LastName = @LastName, 
                    Email = @Email, 
                    ContactNo = @ContactNo 
                WHERE UserId = @UserId";

            using (var db = CreateConnection())
            {
                var rowsAffected = await db.ExecuteAsync(sql, user);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Users WHERE UserId = @Id";
            using (var db = CreateConnection())
            {
                var rowsAffected = await db.ExecuteAsync(sql, new { Id = id });
                return rowsAffected > 0;
            }
        }
    }
}
