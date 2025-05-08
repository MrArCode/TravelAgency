using System.Data.SqlClient;
using TravelAgencyApi.Configuration;
using TravelAgencyApi.Models;

namespace TravelAgencyApi.Services
{
    public class TripService : ITripService
    {
        private readonly string _connectionString;

        public TripService(DatabaseConfig dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync()
        {
            var trips = new List<Trip>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string tripQuery = @"
                    SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople
                    FROM Trip t
                    ORDER BY t.DateFrom";

                using (SqlCommand command = new SqlCommand(tripQuery, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            trips.Add(new Trip
                            {
                                IdTrip = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                DateFrom = reader.GetDateTime(3),
                                DateTo = reader.GetDateTime(4),
                                MaxPeople = reader.GetInt32(5)
                            });
                        }
                    }
                }

                foreach (var trip in trips)
                {
                    string countryQuery = @"
                        SELECT c.Name
                        FROM Country c
                        JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry
                        WHERE ct.IdTrip = @IdTrip";

                    using (SqlCommand command = new SqlCommand(countryQuery, connection))
                    {
                        command.Parameters.AddWithValue("@IdTrip", trip.IdTrip);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                trip.Countries.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }

            return trips;
        }
    }
}