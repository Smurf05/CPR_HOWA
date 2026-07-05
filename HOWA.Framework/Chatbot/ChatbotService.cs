using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Framework.Chatbot
{
    public class ChatbotService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatbotService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Analyzes a natural language text query about the HOWA system and queries database state to reply.
        /// </summary>
        public async Task<string> ProcessQueryAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Hello! How can I assist you today with the House of Worship Attendance System?";

            var cleanMessage = message.Trim().ToLower();

            // 1. General Project and Developer Info
            if (cleanMessage.Contains("developer") || cleanMessage.Contains("author") || cleanMessage.Contains("created by"))
            {
                return "The HOWA Capstone Project was developed by Aldrin Jone Bisin and Zoe Majani for the College of Computing and Information Sciences, Southern Christian College (April 2026).";
            }

            if (cleanMessage.Contains("how to register") || cleanMessage.Contains("registration") || cleanMessage.Contains("register as user"))
            {
                return "To register: \n" +
                       "1. Open the HOWA Mobile Application.\n" +
                       "2. Navigate to the Registration screen.\n" +
                       "3. Fill in your profile details (Username, Name, Email, Contact).\n" +
                       "4. Submit. Your registration is marked 'Pending' until approved by an administrator.\n" +
                       "5. Upon administrator approval, you will be assigned an RFID card, and your mobile check-in QR code will become active.";
            }

            if (cleanMessage.Contains("technology") || cleanMessage.Contains("tech stack") || cleanMessage.Contains("framework"))
            {
                return "HOWA is engineered on a modern cross-platform stack:\n" +
                       "- Frontend: .NET MAUI (.NET 9) supporting Windows and Android\n" +
                       "- Data Layer: Relational SQL Database with Dapper (Lightweight ORM) for high-performance mapping\n" +
                       "- Integrations: RFID Readers (electromagnetic scanning) and QRCoder (QR generation/checking)\n" +
                       "- Design: Material Design visual aesthetics";
            }

            // 2. Statistics Queries (Dynamic SQL/Dapper lookup)
            if (cleanMessage.Contains("attendance rate") || cleanMessage.Contains("statistics") || cleanMessage.Contains("how many present") || cleanMessage.Contains("rate"))
            {
                try
                {
                    var latestEvent = await _unitOfWork.Events.GetActiveOrLatestEventAsync();
                    if (latestEvent == null)
                        return "There are no active worship events registered in the system database.";

                    var stats = await _unitOfWork.Attendance.GetDashboardStatsAsync(latestEvent.EventId);
                    return $"For the event '{latestEvent.EventName}' (scheduled: {latestEvent.EventDate:yyyy-MM-dd HH:mm}):\n" +
                           $"- Total Approved Members: {stats.TotalRegistered}\n" +
                           $"- Attendees Checked In: {stats.TotalPresent}\n" +
                           $"- Pending Approvals: {stats.PendingApprovals}\n" +
                           $"- Attendance Rate: {stats.AttendanceRate}%";
                }
                catch (Exception ex)
                {
                    return $"Unable to fetch dashboard statistics: {ex.Message}";
                }
            }

            // 3. Attendee scan lookup (e.g. "Is John Doe present?")
            var presentRegex = new Regex(@"is\s+([a-zA-Z0-9_\s]+)\s+(present|here|checked\s+in|logged)");
            var match = presentRegex.Match(cleanMessage);
            if (match.Success)
            {
                var nameToSearch = match.Groups[1].Value.Trim();
                try
                {
                    var latestEvent = await _unitOfWork.Events.GetActiveOrLatestEventAsync();
                    if (latestEvent == null)
                        return "There are no active events to search logs for.";

                    var reports = await _unitOfWork.Attendance.GetAttendanceReportAsync(latestEvent.EventId, null);
                    var matchedRecord = reports.FirstOrDefault(r => 
                        r.FirstName.ToLower().Contains(nameToSearch) || 
                        r.LastName.ToLower().Contains(nameToSearch) || 
                        r.FullName.ToLower().Contains(nameToSearch));

                    if (matchedRecord != null)
                    {
                        return $"Yes, {matchedRecord.FullName} is checked in for '{latestEvent.EventName}'. Scanned at {matchedRecord.ScannedAt:HH:mm:ss} using {matchedRecord.AttendanceMethod} (Status: {matchedRecord.AttendanceStatus}).";
                    }
                    else
                    {
                        return $"No attendance scan found for '{nameToSearch}' in the current event '{latestEvent.EventName}'. They might be absent or pending registration.";
                    }
                }
                catch (Exception ex)
                {
                    return $"Error querying attendance record: {ex.Message}";
                }
            }

            // 4. Default Assistant Response
            return "I am the HOWA AI Assistant. How can I help you?\n" +
                   "- Ask about developers ('Who developed this system?')\n" +
                   "- Ask about registration ('How do I register?')\n" +
                   "- Check event attendance rates ('What is the current attendance rate?')\n" +
                   "- Check a person's status ('Is Sam Wilson present?')\n" +
                   "- Learn about technologies ('What is the technology stack?')\n";
        }
    }
}
