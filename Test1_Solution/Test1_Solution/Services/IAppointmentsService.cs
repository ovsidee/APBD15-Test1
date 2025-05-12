using Test1_Solution.Models;
using Test1_Solution.Models.DTO;

namespace Test1_Solution.Services;

public interface IAppointmentsService
{
    public Task<AppointmentDTO?> GetAppointmentByIdAsync(int id, CancellationToken cancellationToken);
    public Task<AddAppointmentResultEnum> AddAppointmentAsync(AddAppointmentDTO addAppointmentDto, CancellationToken cancellationToken);
}