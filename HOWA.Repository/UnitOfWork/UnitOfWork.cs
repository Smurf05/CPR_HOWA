using System;
using HOWA.Domain.Interfaces;
using HOWA.Repository.Repositories;

namespace HOWA.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly string _connectionString;
        private IUserRepository _users;
        private IAttendeeRepository _attendees;
        private IEventRepository _events;
        private IAttendanceRepository _attendance;
        private bool _disposed;

        public UnitOfWork(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IUserRepository Users => _users ??= new UserRepository(_connectionString);

        public IAttendeeRepository Attendees => _attendees ??= new AttendeeRepository(_connectionString);

        public IEventRepository Events => _events ??= new EventRepository(_connectionString);

        public IAttendanceRepository Attendance => _attendance ??= new AttendanceRepository(_connectionString);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources if any
                }
                _disposed = true;
            }
        }
    }
}
