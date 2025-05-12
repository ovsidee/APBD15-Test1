namespace Test1_Solution.Models.DTO;

public class AddAppointmentDTO
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string Pwz { get; set; }
    public List<AddAppointmentServiceDTO> Services { get; set; }
}