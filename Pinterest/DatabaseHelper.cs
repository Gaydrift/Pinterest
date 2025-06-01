using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace Pinterest
{
    public static class DatabaseHelper
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["Pinterest.Properties.Settings.PinterestDBConnectionString"].ConnectionString;

        public static List<ImageMetaData> GetUserImages(int userId)
        {
            var images = new List<ImageMetaData>();

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT i.*
        FROM Images i
        JOIN Users u ON i.UploadedBy = u.Email      -- ← почта!
        WHERE u.Id = @UserId", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                images.Add(new ImageMetaData
                {
                    FileName = reader["FileName"].ToString(),
                    OriginalName = reader["OriginalName"].ToString(),
                    UploadedBy = reader["UploadedBy"].ToString(),
                    Description = reader["Description"].ToString(),
                    IsPremium = Convert.ToBoolean(reader["IsPremium"]),
                    Tags = reader["Tags"]?.ToString()
                                      .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .ToList() ?? new()
                });
            }
            return images;
        }



        public static void SaveImageMeta(ImageMetaData meta)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"INSERT INTO Images (FileName, OriginalName, UploadedBy, Description, IsPremium, Tags)
                                           VALUES (@FileName, @OriginalName, @UploadedBy, @Description, @IsPremium, @Tags)", conn);

                cmd.Parameters.AddWithValue("@FileName", meta.FileName);
                cmd.Parameters.AddWithValue("@OriginalName", meta.OriginalName ?? "");
                cmd.Parameters.AddWithValue("@UploadedBy", meta.UploadedBy ?? "");
                cmd.Parameters.AddWithValue("@Description", meta.Description ?? "");
                cmd.Parameters.AddWithValue("@IsPremium", meta.IsPremium);
                cmd.Parameters.AddWithValue("@Tags", string.Join(",", meta.Tags ?? new List<string>()));

                cmd.ExecuteNonQuery();
            }
        }
        public static int GetUserId(string userName)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT Id FROM Users WHERE Name = @Name", conn);
            cmd.Parameters.AddWithValue("@Name", userName);

            return (int?)cmd.ExecuteScalar() ?? -1;
        }

        public static List<string> GetUserCollections(int userId)
        {
            var collections = new List<string>();

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT Name FROM Collections WHERE UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                collections.Add(reader["Name"].ToString());
            }

            return collections;
        }

        public static void CreateCollection(int userId, string collectionName)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            var cmd = new SqlCommand("INSERT INTO Collections (UserId, Name) VALUES (@UserId, @Name)", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Name", collectionName);
            cmd.ExecuteNonQuery();
        }

        public static List<ImageMetaData> GetImagesFromCollection(int userId, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

            var result = new List<ImageMetaData>();
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT i.*
        FROM Collections c
        JOIN CollectionImages ci ON c.Id = ci.CollectionId
        JOIN Images i ON i.Id = ci.ImageId
        WHERE c.UserId = @UserId AND c.Name = @Name", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Name", collectionName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ImageMetaData
                {
                    FileName = reader["FileName"].ToString(),
                    OriginalName = reader["OriginalName"].ToString(),
                    UploadedBy = reader["UploadedBy"].ToString(),
                    Description = reader["Description"].ToString(),
                    IsPremium = Convert.ToBoolean(reader["IsPremium"]),
                    Tags = reader["Tags"].ToString()
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList() ?? new()
                });
            }

            return result;
        }

        public static void AddImageToCollection(int userId, string collectionName, string fileName)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            var transaction = conn.BeginTransaction();

            try
            {
                var getColIdCmd = new SqlCommand(
                    "SELECT Id FROM Collections WHERE UserId = @UserId AND Name = @Name", conn, transaction);
                getColIdCmd.Parameters.AddWithValue("@UserId", userId);
                getColIdCmd.Parameters.AddWithValue("@Name", collectionName);
                var collectionId = (int?)getColIdCmd.ExecuteScalar();

                if (collectionId == null) throw new Exception("Collection not found");

                var getImgIdCmd = new SqlCommand(
                    "SELECT Id FROM Images WHERE FileName = @FileName", conn, transaction);
                getImgIdCmd.Parameters.AddWithValue("@FileName", fileName);
                var imageId = (int?)getImgIdCmd.ExecuteScalar();

                if (imageId == null) throw new Exception("Image not found");

                var insertCmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM CollectionImages WHERE CollectionId = @ColId AND ImageId = @ImgId) " +
                    "INSERT INTO CollectionImages (CollectionId, ImageId) VALUES (@ColId, @ImgId)",
                    conn, transaction);
                insertCmd.Parameters.AddWithValue("@ColId", collectionId);
                insertCmd.Parameters.AddWithValue("@ImgId", imageId);

                insertCmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public static bool DeleteCollection(int userId, string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty.", nameof(collectionName));

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                var getColIdCmd = new SqlCommand(
                    "SELECT Id FROM Collections WHERE UserId = @UserId AND Name = @Name", conn, transaction);
                getColIdCmd.Parameters.AddWithValue("@UserId", userId);
                getColIdCmd.Parameters.AddWithValue("@Name", collectionName);
                var collectionId = (int?)getColIdCmd.ExecuteScalar();

                if (collectionId == null)
                {
                    transaction.Commit();
                    return false;
                }

                var delLinks = new SqlCommand("DELETE FROM CollectionImages WHERE CollectionId = @Id", conn, transaction);
                delLinks.Parameters.AddWithValue("@Id", collectionId);
                delLinks.ExecuteNonQuery();

                var delCol = new SqlCommand("DELETE FROM Collections WHERE Id = @Id", conn, transaction);
                delCol.Parameters.AddWithValue("@Id", collectionId);
                delCol.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    
                }


                return false;
            }
        }
        public static List<ImageMetaData> GetLikedImagesByUser(string userName)
        {
            var result = new List<ImageMetaData>();

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT i.*
        FROM FeedbackLikes fl
        JOIN ImageFeedback f ON fl.FeedbackId = f.Id
        JOIN Images i ON f.ImageId = i.Id
        JOIN Users u ON fl.UserId = u.Id
        WHERE u.Name = @UserName", conn);

            cmd.Parameters.AddWithValue("@UserName", userName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ImageMetaData
                {
                    FileName = reader["FileName"].ToString(),
                    OriginalName = reader["OriginalName"].ToString(),
                    UploadedBy = reader["UploadedBy"].ToString(),
                    Description = reader["Description"].ToString(),
                    IsPremium = Convert.ToBoolean(reader["IsPremium"]),
                    Tags = reader["Tags"]?.ToString()
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList() ?? new()
                });
            }

            return result;
        }



    }
}
