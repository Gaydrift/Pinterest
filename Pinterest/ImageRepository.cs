using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace Pinterest.Data
{
    public class ImageRepository
    {
        private readonly string _connectionString;
        private readonly UserRepository _userRepository;

        public ImageRepository(string connectionString, UserRepository userRepository)
        {
            _connectionString = connectionString;
            _userRepository = userRepository;
        }


        public List<ImageMetaData> GetAll()
        {
            var list = new List<ImageMetaData>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT * FROM Images", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToImage(reader));
            }
            return list;
        }

        public ImageMetaData GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT * FROM Images WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return MapReaderToImage(reader);
            return null;
        }

        public int Add(ImageMetaData image)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"INSERT INTO Images (FileName, OriginalName, UploadedBy, Description, IsPremium, Tags)
          VALUES (@FileName, @OriginalName, @UploadedBy, @Description, @IsPremium, @Tags);
          SELECT CAST(SCOPE_IDENTITY() AS int);", conn);

            cmd.Parameters.AddWithValue("@FileName", image.FileName);
            cmd.Parameters.AddWithValue("@OriginalName", (object)image.OriginalName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UploadedBy", (object)image.UploadedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object)image.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsPremium", image.IsPremium);
            cmd.Parameters.AddWithValue("@Tags", image.Tags != null ? string.Join(",", image.Tags) : (object)DBNull.Value);

            conn.Open();
            int newId = (int)cmd.ExecuteScalar();
            image.Id = newId;
            return newId;
        }


        public void Update(ImageMetaData image)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"UPDATE Images SET FileName = @FileName, OriginalName = @OriginalName, UploadedBy = @UploadedBy,
                  Description = @Description, IsPremium = @IsPremium, Tags = @Tags WHERE Id = @Id", conn);

            cmd.Parameters.AddWithValue("@FileName", image.FileName);
            cmd.Parameters.AddWithValue("@OriginalName", (object)image.OriginalName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UploadedBy", (object)image.UploadedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object)image.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsPremium", image.IsPremium);
            cmd.Parameters.AddWithValue("@Tags", image.Tags != null ? string.Join(",", image.Tags) : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", image.Id);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("DELETE FROM Images WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<ImageMetaData> Search(string authorFilter, string tagFilter)
        {
            var list = new List<ImageMetaData>();
            using var conn = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM Images WHERE 1=1";
            if (!string.IsNullOrEmpty(authorFilter))
                sql += " AND UploadedBy LIKE @Author";
            if (!string.IsNullOrEmpty(tagFilter))
                sql += " AND Tags LIKE @Tag";
            using var cmd = new SqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(authorFilter))
                cmd.Parameters.AddWithValue("@Author", $"%{authorFilter}%");
            if (!string.IsNullOrEmpty(tagFilter))
                cmd.Parameters.AddWithValue("@Tag", $"%{tagFilter}%");
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToImage(reader));
            }
            return list;
        }

        private ImageMetaData MapReaderToImage(SqlDataReader reader)
        {
            var uploadedBy = reader["UploadedBy"] as string;

            var author = _userRepository.GetUserByLogin(uploadedBy);

            var tagsStr = reader["Tags"] as string ?? "";
            return new ImageMetaData
            {
                Id = (int)reader["Id"],
                FileName = reader["FileName"] as string,
                OriginalName = reader["OriginalName"] as string,
                UploadedBy = uploadedBy,
                AuthorDisplayName = author?.Name ?? "Неизвестный автор",
                Description = reader["Description"] as string,
                IsPremium = (bool)reader["IsPremium"],
                Tags = string.IsNullOrEmpty(tagsStr)
                    ? new List<string>()
                    : new List<string>(tagsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            };
        }

    }
}
