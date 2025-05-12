using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Test1_Solution.Models;
using Test1_Solution.Models.DTO;
using Test1_Solution.Services;

namespace Test1_Solution.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _appointmentsService;
    
    public AppointmentsController(IAppointmentsService appointmentsService)
    {
        _appointmentsService = appointmentsService;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointmentByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (id < 0)
            return BadRequest("Id must be positive.");
        
        var result = await _appointmentsService.GetAppointmentByIdAsync(id, cancellationToken);
        
        if (result == null)
            return NotFound($"Appointment with id: \"{id}\" not found.");
        
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddAppointmentAsync([FromBody] AddAppointmentDTO addAppointmentDto, CancellationToken cancellationToken)
    {
        if (addAppointmentDto.AppointmentId < 0 || addAppointmentDto.PatientId < 0 || addAppointmentDto.Pwz.IsNullOrEmpty() || addAppointmentDto.Services.Count == 0)
            return BadRequest("Invalid data.");
        
        var result = await _appointmentsService.AddAppointmentAsync(addAppointmentDto, cancellationToken);

        switch (result)
        {
            case AddAppointmentResultEnum.Success:
                return Created();
            case AddAppointmentResultEnum.AppointmentExists:
                return Conflict("Appointment already exists.");
            case AddAppointmentResultEnum.PatientNotFound:
                return NotFound("Patient not found.");
            case AddAppointmentResultEnum.DoctorNotFound:
                return NotFound("Doctor not found with this PWZ.");
            case AddAppointmentResultEnum.ServiceNotFound:
                return NotFound("One Service or more not found.");
            default:
                return BadRequest("Unknown error.");
        }
    }
}