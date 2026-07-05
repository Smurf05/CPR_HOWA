using System;
using HOWA.Domain.Interfaces;

namespace HOWA.Repository.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IAttendeeRepository Attendees { get; }
        IEventRepository Events { get; }
        IAttendanceRepository Attendance { get; }
    }
}
