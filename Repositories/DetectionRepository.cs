// Data/DetectionRepository.cs
using CustomIdentity.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace CustomIdentity.Data
{
    public class DetectionRepository
    {
        private readonly string _connectionString;

        public DetectionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<DetectionBatch> GetBatches(int lastBatchId)
        {
            List<DetectionBatch> batches = new List<DetectionBatch>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM ObjectDetections WHERE BatchID > @LastBatchId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LastBatchId", lastBatchId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DetectionBatch batch = new DetectionBatch
                            {
                                BatchID = reader.GetInt32(reader.GetOrdinal("BatchID")),
                                VideoID = reader.IsDBNull(reader.GetOrdinal("VideoID")) ? null : reader.GetString(reader.GetOrdinal("VideoID")),
                                ObjectType = reader.IsDBNull(reader.GetOrdinal("ObjectType")) ? null : reader.GetString(reader.GetOrdinal("ObjectType")),
                                Probability = reader.IsDBNull(reader.GetOrdinal("Probability")) ? (float?)null : (float)reader.GetDouble(reader.GetOrdinal("Probability")),
                                DetectionTime = reader.IsDBNull(reader.GetOrdinal("DetectionTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DetectionTime")),
                                ObjectCount = reader.IsDBNull(reader.GetOrdinal("ObjectCount")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ObjectCount")),
                                SessionID = reader.IsDBNull(reader.GetOrdinal("SessionID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("SessionID")),
                                LocationX = reader.IsDBNull(reader.GetOrdinal("LocationX")) ? (float?)null : (float)reader.GetDouble(reader.GetOrdinal("LocationX")),
                                LocationY = reader.IsDBNull(reader.GetOrdinal("LocationY")) ? (float?)null : (float)reader.GetDouble(reader.GetOrdinal("LocationY"))
                            };
                            batches.Add(batch);
                        }
                    }
                }
            }
            Console.WriteLine($"Fetched {batches.Count} batches with Batch ID greater than {lastBatchId}"); // Debug log
            return batches;
        }


        // New method to get the latest batch for a given SessionID and sum ObjectCounts by ObjectType
        public Dictionary<string, int> GetLatestBatchSummary(int sessionId)
        {
            var summary = new Dictionary<string, int>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT ObjectType, SUM(ObjectCount) AS TotalCount
                    FROM ObjectDetections
                    WHERE SessionID = @SessionID
                    AND BatchID = (SELECT MAX(BatchID) FROM ObjectDetections WHERE SessionID = @SessionID)
                    GROUP BY ObjectType";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionID", sessionId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string objectType = reader.GetString(reader.GetOrdinal("ObjectType"));
                            int totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                            summary[objectType] = totalCount; // Add to the dictionary
                        }
                    }
                }
            }

            return summary;
        }
    }
}
