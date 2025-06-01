using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Pinterest
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void AddUser(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
        INSERT INTO Users (Name, Email, Password, Role, IsAdmin, IsBanned)
        OUTPUT INSERTED.Id
        VALUES (@Name, @Email, @Password, @Role, @IsAdmin, @IsBanned)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", user.Name);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.Parameters.AddWithValue("@Role", user.Role);
            command.Parameters.AddWithValue("@IsAdmin", user.IsAdmin);
            command.Parameters.AddWithValue("@IsBanned", user.IsBanned);

            user.Id = (int)command.ExecuteScalar();
        }

        public void UpdateUser(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"UPDATE Users SET Name = @Name, Email = @Email, Password = @Password, Role = @Role,
                  IsAdmin = @IsAdmin, IsBanned = @IsBanned, CanUploadImages = @CanUpload WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@Name", user.Name);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.Parameters.AddWithValue("@Role", user.Role);
            command.Parameters.AddWithValue("@IsAdmin", user.IsAdmin);
            command.Parameters.AddWithValue("@IsBanned", user.IsBanned);
            command.Parameters.AddWithValue("@CanUpload", user.CanUpload);

            command.ExecuteNonQuery();
        }

        public void SetUploadPermission(string email, bool canUpload)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("UPDATE Users SET CanUploadImages = @CanUpload WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@CanUpload", canUpload);
            command.Parameters.AddWithValue("@Email", email);

            command.ExecuteNonQuery();
        }

        public User GetUserByLogin(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = "SELECT * FROM Users WHERE Email = @Email";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"]?.ToString(),
                    Email = reader["Email"]?.ToString(),
                    Password = reader["Password"]?.ToString(),
                    Role = reader["Role"]?.ToString(),
                    IsAdmin = (bool)reader["IsAdmin"],
                    IsBanned = (bool)reader["IsBanned"],
                    CanUpload = (bool)reader["CanUploadImages"]
                };
            }

            return null;
        }

        public List<User> GetAllUsers()
        {
            var users = new List<User>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = "SELECT * FROM Users";

            using var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new User
                {
                    Name = reader["Name"]?.ToString(),
                    Email = reader["Email"]?.ToString(),
                    Password = reader["Password"]?.ToString(),
                    Role = reader["Role"]?.ToString(),
                    IsAdmin = (bool)reader["IsAdmin"],
                    IsBanned = (bool)reader["IsBanned"],
                    CanUpload = (bool)reader["CanUploadImages"]
                });
            }

            return users;
        }
    }
}
