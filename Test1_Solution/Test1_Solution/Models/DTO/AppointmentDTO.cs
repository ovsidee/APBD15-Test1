namespace Test1_Solution.Models.DTO;

public class AppointmentDTO
{
    public DateTime Date { get; set; }
    public PatientDTO Patient { get; set; }
    public DoctorDTO Doctor { get; set; }
    public List<AppointmentServiceDTO> AppointmentServices { get; set; }
}