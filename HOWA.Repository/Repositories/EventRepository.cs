using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using HOWA.Domain.Interfaces;
using HOWA.Domain.Models;

namespace HOWA.Repository.Repositories
{
    public class EventRepository : BaseRepository, IEventRepository
    {
        public EventRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<Event> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Events WHERE EventId = @Id";
            using (var db = CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<Event>(sql, new { Id = id });
            }
        }

        public async Task<IEnumerable<Event>> GetAllAsync()
        {
            const string sql = "SELECT * FROM Events ORDER BY EventDate DESC";
            using (var db = CreateConnection())
            {
                return await db.QueryAsync<Event>(sql);
            }
        }

        public async Task<Event> GetActiveOrLatestEventAsync()
        {
            // Gets event happening today or the latest event
            const string sql = @"
                SELECT TOP 1 * FROM Events 
                ORDER BY ABS(DATEDIFF(minute, EventDate, GETDATE())) ASC";

            using (var db = CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<Event>(sql);
            }
        }

        public async Task<bool> AddAsync(Event ev)
        {
            const string sql = @"
                INSERT INTO Events (EventName, EventDate, Description, CreatedAt)
                VALUES (@EventName, @EventDate, @Description, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using (var db = CreateConnection())
            {
                var id = await db.QuerySingleAsync<int>(sql, ev);
                if (id > 0)
                {
                    ev.EventId = id;
                    return true;
                }
                return false;
            }
        }
    }
}
