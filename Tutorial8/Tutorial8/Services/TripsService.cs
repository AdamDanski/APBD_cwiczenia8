using Microsoft.Data.SqlClient;
using Tutorial8.Controllers;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString;

    public TripsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        const string sql = @"
        SELECT 
        T.IdTrip, T.Name, C.Name AS CountryName
        FROM Trip T
        LEFT JOIN Country_Trip CT ON T.IdTrip = CT.IdTrip
        LEFT JOIN Country C ON C.IdCountry = CT.IdCountry
        ORDER BY T.IdTrip
    ";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        var tripDict = new Dictionary<int, TripDTO>();

        while (await reader.ReadAsync())
        {
            int idTrip = reader.GetInt32(0);
            string tripName = reader.GetString(1);
            string? countryName = reader.IsDBNull(2) ? null : reader.GetString(2);

            if (!tripDict.ContainsKey(idTrip))
            {
                tripDict[idTrip] = new TripDTO
                {
                    Id = idTrip,
                    Name = tripName,
                    Countries = new List<CountryDTO>()
                };
            }

            if (countryName is not null)
            {
                tripDict[idTrip].Countries.Add(new CountryDTO
                {
                    Name = countryName
                });
            }
        }

        return tripDict.Values.ToList();
    }

    public async Task<List<ClientTripDTO>> GetClientTrips(int clientId)
    {
        var result = new List<ClientTripDTO>();

        const string sql = """
                               SELECT T.Name AS TripName, C.Name AS CountryName
                               FROM Client_Trip CT
                               JOIN Trip T ON CT.IdTrip = T.IdTrip
                               JOIN Country_Trip CT2 ON T.IdTrip = CT2.IdTrip
                               JOIN Country C ON C.IdCountry = CT2.IdCountry
                               WHERE CT.IdClient = @IdClient
                               ORDER BY T.Name;
                           """;

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdClient", clientId);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        var dict = new Dictionary<string, ClientTripDTO>();

        while (await reader.ReadAsync())
        {
            var tripName = reader.GetString(0);
            var countryName = reader.GetString(1);

            if (!dict.ContainsKey(tripName))
            {
                dict[tripName] = new ClientTripDTO
                {
                    TripName = tripName,
                    Countries = new List<string>()
                };
            }

            dict[tripName].Countries.Add(countryName);
        }

        return dict.Values.ToList();
    }
    public async Task AddClientAsync(ClientDTO client)
    {
        const string query = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(query, connection);
    
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Pesel", client.Pesel ?? (object)DBNull.Value);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
    public async Task EnrollClientToTrip(int clientId, int tripId)
    {
        const string checkClientSql = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
        const string checkTripSql = "SELECT 1 FROM Trip WHERE IdTrip = @IdTrip";
        const string checkEnrollmentSql = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
        const string insertSql = @"
        INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
        VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL)";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("", conn);
        await conn.OpenAsync();
        
        cmd.CommandText = checkClientSql;
        cmd.Parameters.AddWithValue("@IdClient", clientId);
        var clientExists = await cmd.ExecuteScalarAsync();
        if (clientExists is null)
            throw new NotFoundException("Klient nie istnieje");

        cmd.Parameters.Clear();
        
        cmd.CommandText = checkTripSql;
        cmd.Parameters.AddWithValue("@IdTrip", tripId);
        var tripExists = await cmd.ExecuteScalarAsync();
        if (tripExists is null)
            throw new NotFoundException("Wycieczka nie istnieje");

        cmd.Parameters.Clear();

        cmd.CommandText = checkEnrollmentSql;
        cmd.Parameters.AddWithValue("@IdClient", clientId);
        cmd.Parameters.AddWithValue("@IdTrip", tripId);
        var alreadyEnrolled = await cmd.ExecuteScalarAsync();
        if (alreadyEnrolled is not null)
            throw new InvalidOperationException("Klient już zapisany na tę wycieczkę");

        cmd.Parameters.Clear();

        cmd.CommandText = insertSql;
        cmd.Parameters.AddWithValue("@IdClient", clientId);
        cmd.Parameters.AddWithValue("@IdTrip", tripId);
        cmd.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));


        await cmd.ExecuteNonQueryAsync();
    }
    public async Task RemoveClientFromTrip(int clientId, int tripId)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("", conn);

        cmd.CommandText = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
        cmd.Parameters.AddWithValue("@IdClient", clientId);
        var clientExists = await cmd.ExecuteScalarAsync();
        if (clientExists is null)
            throw new NotFoundException("Klient nie istnieje");

        cmd.Parameters.Clear();

        cmd.CommandText = "SELECT 1 FROM Trip WHERE IdTrip = @IdTrip";
        cmd.Parameters.AddWithValue("@IdTrip", tripId);
        var tripExists = await cmd.ExecuteScalarAsync();
        if (tripExists is null)
            throw new NotFoundException("Wycieczka nie istnieje");

        cmd.Parameters.Clear();

        cmd.CommandText = "SELECT PaymentDate FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
        cmd.Parameters.AddWithValue("@IdClient", clientId);
        cmd.Parameters.AddWithValue("@IdTrip", tripId);

        var result = await cmd.ExecuteReaderAsync();
        if (!await result.ReadAsync())
            throw new InvalidOperationException("Klient nie jest zapisany na tę wycieczkę");

        if (!result.IsDBNull(0))
            throw new InvalidOperationException("Nie można usunąć — klient już zapłacił");

        await result.CloseAsync();
        cmd.Parameters.Clear();

        cmd.CommandText = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
        cmd.Parameters.AddWithValue("@IdClient", clientId);
        cmd.Parameters.AddWithValue("@IdTrip", tripId);

        await cmd.ExecuteNonQueryAsync();
    }


}