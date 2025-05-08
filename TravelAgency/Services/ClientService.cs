using System.Data.SqlClient;
using TravelAgencyApi.Configuration;
using TravelAgencyApi.Models;

namespace TravelAgencyApi.Services
{
    public class ClientService : IClientService
    {
        private readonly string _connectionString;

        public ClientService(DatabaseConfig dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
        }

        public async Task<int> CreateClientAsync(Client client)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string insertQuery = @"
                    INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                    VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
                    SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", client.FirstName);
                    command.Parameters.AddWithValue("@LastName", client.LastName);
                    command.Parameters.AddWithValue("@Email", client.Email);
                    command.Parameters.AddWithValue("@Telephone", (object)client.Telephone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Pesel", (object)client.Pesel ?? DBNull.Value);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<bool> ClientExistsAsync(int clientId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    var result = await command.ExecuteScalarAsync();
                    return result != null;
                }
            }
        }

        public async Task<IEnumerable<ClientTrip>> GetClientTripsAsync(int clientId)
        {
            var trips = new List<ClientTrip>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, ct.RegisteredAt, ct.PaymentDate
                    FROM Trip t
                    JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
                    WHERE ct.IdClient = @IdClient
                    ORDER BY t.DateFrom";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            trips.Add(new ClientTrip
                            {
                                IdTrip = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                DateFrom = reader.GetDateTime(3),
                                DateTo = reader.GetDateTime(4),
                                RegisteredAt = reader.GetInt32(5),
                                PaymentDate = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6)
                            });
                        }
                    }
                }
            }

            return trips;
        }

        public async Task<bool> RegisterClientForTripAsync(int clientId, int tripId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        bool clientExists;
                        using (SqlCommand command = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @IdClient",
                                   connection, transaction))
                        {
                            command.Parameters.AddWithValue("@IdClient", clientId);
                            var result = await command.ExecuteScalarAsync();
                            clientExists = result != null;
                        }

                        if (!clientExists)
                        {
                            transaction.Rollback();
                            return false;
                        }
                        
                        bool tripExists;
                        using (SqlCommand command = new SqlCommand("SELECT 1 FROM Trip WHERE IdTrip = @IdTrip",
                                   connection, transaction))
                        {
                            command.Parameters.AddWithValue("@IdTrip", tripId);
                            var result = await command.ExecuteScalarAsync();
                            tripExists = result != null;
                        }

                        if (!tripExists)
                        {
                            transaction.Rollback();
                            return false;
                        }
                        
                        bool registrationExists;
                        using (SqlCommand command = new SqlCommand(
                                   "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip",
                                   connection, transaction))
                        {
                            command.Parameters.AddWithValue("@IdClient", clientId);
                            command.Parameters.AddWithValue("@IdTrip", tripId);
                            var result = await command.ExecuteScalarAsync();
                            registrationExists = result != null;
                        }

                        if (registrationExists)
                        {
                            transaction.Rollback();
                            return false;
                        }
                        
                        int currentEnrollments;
                        int maxPeople;
                        using (SqlCommand command = new SqlCommand(@"
                            SELECT 
                                (SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip) AS CurrentEnrollments,
                                (SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip) AS MaxPeople",
                                   connection, transaction))
                        {
                            command.Parameters.AddWithValue("@IdTrip", tripId);
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    currentEnrollments = reader.GetInt32(0);
                                    maxPeople = reader.GetInt32(1);
                                }
                                else
                                {
                                    transaction.Rollback();
                                    return false;
                                }
                            }
                        }

                        if (currentEnrollments >= maxPeople)
                        {
                            transaction.Rollback();
                            return false;
                        }
                        
                        int registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                        
                        using (SqlCommand command = new SqlCommand(@"
                            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                            VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL)",
                                   connection, transaction))
                        {
                            command.Parameters.AddWithValue("@IdClient", clientId);
                            command.Parameters.AddWithValue("@IdTrip", tripId);
                            command.Parameters.AddWithValue("@RegisteredAt", registeredAt);

                            await command.ExecuteNonQueryAsync();
                        }
                        
                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> RemoveClientFromTripAsync(int clientId, int tripId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                bool registrationExists;
                using (SqlCommand command = new SqlCommand(
                           "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip",
                           connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdTrip", tripId);
                    var result = await command.ExecuteScalarAsync();
                    registrationExists = result != null;
                }

                if (!registrationExists)
                {
                    return false;
                }
                
                using (SqlCommand command = new SqlCommand(
                           "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip",
                           connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdTrip", tripId);
                    await command.ExecuteNonQueryAsync();
                }

                return true;
            }
        }
    }
}