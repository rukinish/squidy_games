using UnityEngine;
using Mono.Data.Sqlite;
using System;
using System.IO;
using System.Collections.Generic;

public class DataSaver : MonoBehaviour
{
    private string dbPath;

    void Awake()
    {
        dbPath = "URI=file:" + Path.Combine(Application.persistentDataPath, "game_data_v2.db");
        Debug.Log($"[DatabaseManager] Using DB path: {dbPath}");
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS FocusSessions (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            session_id TEXT,
                            date TEXT,
                            duration REAL,
                            gaze_on_target REAL,
                            longest_focus REAL,
                            break_count INTEGER,
                            off_screen_count INTEGER,
                            time_to_first_fixation REAL DEFAULT 0,
                            heatmap_path TEXT,
                            teacher_notes TEXT
                        );
                    ";
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    try {
                        command.CommandText = "ALTER TABLE FocusSessions ADD COLUMN time_to_first_fixation REAL DEFAULT 0;";
                        command.ExecuteNonQuery();
                        Debug.Log("[DatabaseManager] Migrated FocusSessions: added time_to_first_fixation.");
                    } catch { /* Column already exists — safe to ignore */ }
                }

                using (var command = connection.CreateCommand())
                {
                    try {
                        command.CommandText = "ALTER TABLE QuizSessions ADD COLUMN time_to_first_fixation REAL DEFAULT 0;";
                        command.ExecuteNonQuery();
                        Debug.Log("[DatabaseManager] Migrated QuizSessions: added time_to_first_fixation.");
                    } catch { /* Column already exists — safe to ignore */ }
                }
                using (var command = connection.CreateCommand())
                {
                    try {
                        command.CommandText = "ALTER TABLE QuizSessions ADD COLUMN avg_dwell_duration REAL DEFAULT 0;";
                        command.ExecuteNonQuery();
                        Debug.Log("[DatabaseManager] Migrated QuizSessions: added avg_dwell_duration.");
                    } catch { /* Column already exists — safe to ignore */ }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS QuizSessions (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            session_id TEXT,
                            date TEXT,
                            duration REAL,
                            score REAL,
                            total_attempts INTEGER,
                            correct_answers INTEGER,
                            false_activations INTEGER,
                            total_response_time REAL,
                            time_to_first_fixation REAL,
                            avg_dwell_duration REAL,
                            heatmap_path TEXT,
                            teacher_notes TEXT
                        );
                    ";
                    command.ExecuteNonQuery();
                }

                connection.Close();
                Debug.Log($"[DatabaseManager] Database initialized successfully at {Application.persistentDataPath}/game_data.db");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DatabaseManager] Database creation error: {e.Message}");
        }
    }

    public void InsertFocusSession(string sessionId, string date, float duration, float gazeOnTarget, float longestFocus, int breakCount, int offScreenCount, float timeToFirstFixation, string heatmapPath, string teacherNotes = "")
    {
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO FocusSessions (session_id, date, duration, gaze_on_target, longest_focus, break_count, off_screen_count, time_to_first_fixation, heatmap_path, teacher_notes)
                        VALUES (@sessionId, @date, @duration, @gazeOnTarget, @longestFocus, @breakCount, @offScreenCount, @timeToFirstFixation, @heatmapPath, @teacherNotes);
                    ";

                    command.Parameters.Add(new SqliteParameter("@sessionId", sessionId));
                    command.Parameters.Add(new SqliteParameter("@date", date));
                    command.Parameters.Add(new SqliteParameter("@duration", duration));
                    command.Parameters.Add(new SqliteParameter("@gazeOnTarget", gazeOnTarget));
                    command.Parameters.Add(new SqliteParameter("@longestFocus", longestFocus));
                    command.Parameters.Add(new SqliteParameter("@breakCount", breakCount));
                    command.Parameters.Add(new SqliteParameter("@offScreenCount", offScreenCount));
                    command.Parameters.Add(new SqliteParameter("@timeToFirstFixation", timeToFirstFixation));
                    command.Parameters.Add(new SqliteParameter("@heatmapPath", heatmapPath));
                    command.Parameters.Add(new SqliteParameter("@teacherNotes", teacherNotes));

                    command.ExecuteNonQuery();
                    Debug.Log($"[DatabaseManager] Successfully inserted Focus Session: {sessionId} into DB at {dbPath}.");
                }
                connection.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DatabaseManager] InsertFocusSession error: {e.Message}");
        }
    }

    public void InsertQuizSession(string sessionId, string date, float duration, float score, int totalAttempts, int correctAnswers, int falseActivations, float totalResponseTime, float timeToFirstFixation, float avgDwell, string heatmapPath, string teacherNotes = "")
    {
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO QuizSessions (session_id, date, duration, score, total_attempts, correct_answers, false_activations, total_response_time, time_to_first_fixation, avg_dwell_duration, heatmap_path, teacher_notes)
                        VALUES (@sessionId, @date, @duration, @score, @totalAttempts, @correctAnswers, @falseActivations, @totalResponseTime, @timeToFirstFixation, @avgDwell, @heatmapPath, @teacherNotes);
                    ";

                    command.Parameters.Add(new SqliteParameter("@sessionId", sessionId));
                    command.Parameters.Add(new SqliteParameter("@date", date));
                    command.Parameters.Add(new SqliteParameter("@duration", duration));
                    command.Parameters.Add(new SqliteParameter("@score", score));
                    command.Parameters.Add(new SqliteParameter("@totalAttempts", totalAttempts));
                    command.Parameters.Add(new SqliteParameter("@correctAnswers", correctAnswers));
                    command.Parameters.Add(new SqliteParameter("@falseActivations", falseActivations));
                    command.Parameters.Add(new SqliteParameter("@totalResponseTime", totalResponseTime));
                    command.Parameters.Add(new SqliteParameter("@timeToFirstFixation", timeToFirstFixation));
                    command.Parameters.Add(new SqliteParameter("@avgDwell", avgDwell));
                    command.Parameters.Add(new SqliteParameter("@heatmapPath", heatmapPath));
                    command.Parameters.Add(new SqliteParameter("@teacherNotes", teacherNotes));

                    command.ExecuteNonQuery();
                    Debug.Log($"[DatabaseManager] Successfully inserted Quiz Session: {sessionId} into DB at {dbPath}.");
                }
                connection.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DatabaseManager] InsertQuizSession error: {e.Message}");
        }
    }

    public List<Dictionary<string, string>> GetFocusSessions()
    {
        var result = new List<Dictionary<string, string>>();
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM FocusSessions ORDER BY id DESC;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, string>
                            {
                                {"id", reader["id"].ToString()},
                                {"date", reader["date"].ToString()},
                                {"duration", reader["duration"].ToString()},
                                {"gaze_on_target", reader["gaze_on_target"].ToString()},
                                {"longest_focus", reader["longest_focus"].ToString()},
                                {"break_count", reader["break_count"].ToString()},
                                {"off_screen_count", reader["off_screen_count"].ToString()},
                                {"time_to_first_fixation", reader["time_to_first_fixation"].ToString()},
                                {"heatmap_path", reader["heatmap_path"].ToString()},
                                {"teacher_notes", reader["teacher_notes"].ToString()}
                            };
                            result.Add(row);
                        }
                    }
                }
                connection.Close();
                Debug.Log($"[DatabaseManager] Fetched {result.Count} Focus session(s) from DB at {dbPath}.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DatabaseManager] GetFocusSessions error: {e.Message}");
        }
        return result;
    }

    public List<Dictionary<string, string>> GetQuizSessions()
    {
        var result = new List<Dictionary<string, string>>();
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM QuizSessions ORDER BY id DESC;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, string>
                            {
                                {"id", reader["id"].ToString()},
                                {"date", reader["date"].ToString()},
                                {"duration", reader["duration"].ToString()},
                                {"score", reader["score"].ToString()},
                                {"total_attempts", reader["total_attempts"].ToString()},
                                {"correct_answers", reader["correct_answers"].ToString()},
                                {"false_activations", reader["false_activations"].ToString()},
                                {"total_response_time", reader["total_response_time"].ToString()},
                                {"time_to_first_fixation", reader["time_to_first_fixation"].ToString()},
                                {"avg_dwell_duration", reader["avg_dwell_duration"].ToString()},
                                {"heatmap_path", reader["heatmap_path"].ToString()},
                                {"teacher_notes", reader["teacher_notes"].ToString()}
                            };
                            result.Add(row);
                        }
                    }
                }
                connection.Close();
                Debug.Log($"[DatabaseManager] Fetched {result.Count} Quiz session(s) from DB at {dbPath}.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DatabaseManager] GetQuizSessions error: {e.Message}");
        }
        return result;
    }
}
