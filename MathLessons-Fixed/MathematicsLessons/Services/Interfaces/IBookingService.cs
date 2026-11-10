using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Services.Interfaces;

public interface IBookingService
{
    Task<List<AppointmentPlan>> GetDurationPlansAsync();
    Task<int> SessionsUsedThisMonthAsync(string userId);
    Task<(bool Ok, string Message, Appointment? Appointment)> CompletePaidBookingAsync(
        User user, DateTime start, int planId, string? notes);
    Task<(bool Ok, string Message)> TryJoinAsync(Appointment appt, DateTime now);
    Task MarkMissedIfLateAsync(Appointment appt, DateTime now);
    Task<int> CountSessionsOnDayAsync(DateTime day);
    Task<List<string>> GetAvailableHoursAsync(DateTime day, int durationMinutes);
}
