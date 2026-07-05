using System.Collections.Generic;
using System.Threading.Tasks;
using HOWA.Domain.Models;

namespace HOWA.Domain.Interfaces
{
    public interface IEventRepository
    {
        Task<Event> GetByIdAsync(int id);
        Task<IEnumerable<Event>> GetAllAsync();
        Task<Event> GetActiveOrLatestEventAsync();
        Task<bool> AddAsync(Event ev);
    }
}
