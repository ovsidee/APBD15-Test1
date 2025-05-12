using Microsoft.Data.SqlClient;
using Test1_Solution.Models;
using Test1_Solution.Models.DTO;

namespace Test1_Solution.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly IConfiguration _configuration;

    public AppointmentsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<AppointmentDTO?> GetAppointmentByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("ConnectionString"));

        AppointmentDTO? appointmentDto = null;
        
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"
                            SELECT a.date, p.first_name, p.last_name, p.date_of_birth, d.doctor_id, d.PWZ, s.name, aps.service_fee
                            FROM Appointment a
                            JOIN Patient p on a.patient_id = p.patient_id
                            JOIN Doctor d on a.doctor_id = d.doctor_id
                            LEFT JOIN Appointment_Service aps on a.appointment_id = aps.appointment_id
                            LEFT JOIN Service s on aps.service_id = s.service_id
                            WHERE a.appointment_id = @id;";
            
            com.Parameters.AddWithValue("@id", id);
            
            await con.OpenAsync(cancellationToken);
            
            var reader = await com.ExecuteReaderAsync(cancellationToken);
            
            if (!reader.HasRows)
                return appointmentDto;
            
            List<AppointmentServiceDTO> AppointmentServices = new List<AppointmentServiceDTO>();
           
            while (await reader.ReadAsync(cancellationToken))
            {
                var dateAppointment = (DateTime)reader["date"];
                var firstName = (string)reader["first_name"];
                var lastName = (string)reader["last_name"];
                var dateOfBirth = (DateTime)reader["date_of_birth"];
                var doctorId = (int)reader["doctor_id"];
                var PWZ = (string)reader["PWZ"];
                var serviceName = (string)reader["name"];
                var serviceFee = (decimal)reader["service_fee"];
                
                if (appointmentDto == null)
                {
                    appointmentDto = new AppointmentDTO()
                    {
                        Date = dateAppointment,
                        Patient = new PatientDTO()
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            DateOfBirth = dateOfBirth
                        },
                        Doctor = new DoctorDTO()
                        {
                            DoctorId = doctorId,
                            Pwz = PWZ
                        },
                        AppointmentServices = AppointmentServices
                    };
                }
                AppointmentServices.Add(new AppointmentServiceDTO()
                {
                    Name = serviceName,
                    ServiceFee = serviceFee
                });
            }
        };
        return appointmentDto;
    }

    public async Task<AddAppointmentResultEnum> AddAppointmentAsync(AddAppointmentDTO addAppointmentDto, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("ConnectionString"));
        
        await con.OpenAsync(cancellationToken);
        
        //check a visit 
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"SELECT COUNT(*) FROM Appointment WHERE appointment_id = @id;";
            com.Parameters.AddWithValue("@id", addAppointmentDto.AppointmentId);
            
            var result = await com.ExecuteScalarAsync(cancellationToken);
            
            if ((int)result > 0)
                return AddAppointmentResultEnum.AppointmentExists;
        }
        
        //check a patient
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"SELECT COUNT(*) FROM Patient WHERE patient_id = @id;";
            com.Parameters.AddWithValue("@id", addAppointmentDto.PatientId);
            
            var result = await com.ExecuteScalarAsync(cancellationToken);
            
            if ((int)result == 0)
                return AddAppointmentResultEnum.PatientNotFound;
        }
        
        //check a doctor
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"SELECT COUNT(*) FROM Doctor WHERE PWZ = @pwz;";
            com.Parameters.AddWithValue("@pwz", addAppointmentDto.Pwz);
            
            var result = await com.ExecuteScalarAsync(cancellationToken);
            
            if ((int)result == 0)
                return AddAppointmentResultEnum.DoctorNotFound;
        }
        
        //check service with name
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            foreach (var nameOfService in addAppointmentDto.Services)
            {
                com.Parameters.Clear();
                com.CommandText = @"SELECT COUNT(*) FROM Service WHERE name = @name;";
                com.Parameters.AddWithValue("@name", nameOfService.ServiceName);
                
                var result = (int) await com.ExecuteScalarAsync(cancellationToken);
                if (result == 0)
                    return AddAppointmentResultEnum.ServiceNotFound;
            }
        }

        //insert into Appointment
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            //get doctor id
            int doctorId;
            await using (SqlCommand com2 = new SqlCommand())
            {
                com2.Connection = con;
                com2.CommandText = @"SELECT doctor_id FROM Doctor WHERE PWZ = @pwz;";
                com2.Parameters.AddWithValue("@pwz", addAppointmentDto.Pwz);
                
                doctorId = (int) await com2.ExecuteScalarAsync(cancellationToken);
            }
            
            com.CommandText = @"INSERT INTO Appointment (appointment_id, patient_id, doctor_id, date)
                                VALUES (@id, @patientId, @doctorId, GETDATE());";
            com.Parameters.AddWithValue("@id", addAppointmentDto.AppointmentId);
            com.Parameters.AddWithValue("@patientId", addAppointmentDto.PatientId);
            com.Parameters.AddWithValue("@doctorId", doctorId);
            
            await com.ExecuteNonQueryAsync(cancellationToken);
        }
        
        //insert into Appointment_Service 
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            
            foreach (var service in addAppointmentDto.Services)
            {
                //get service id
                int serviceIdToInsert;
                await using (SqlCommand com2 = new SqlCommand())
                {
                    com2.Parameters.Clear();
                    com2.Connection = con;
                    com2.CommandText = @"SELECT service_id FROM Service WHERE name = @name;";
                    com2.Parameters.AddWithValue("@name", service.ServiceName);
                    
                    serviceIdToInsert = (int) await com2.ExecuteScalarAsync(cancellationToken);
                }
                com.Parameters.Clear();
                com.CommandText = @"INSERT INTO Appointment_Service (appointment_id, service_id, service_fee)
                                    VALUES (@id, @serviceId, @serviceFee);";
                
                com.Parameters.AddWithValue("@id", addAppointmentDto.AppointmentId);
                com.Parameters.AddWithValue("@serviceId", serviceIdToInsert);
                com.Parameters.AddWithValue("@serviceFee", service.ServiceFee);
                
                await com.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        return AddAppointmentResultEnum.Success; 
    }
}