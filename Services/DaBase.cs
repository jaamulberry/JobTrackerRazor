using System.Data.SQLite;
using JobAppBlazorWeb.Models;

namespace JobAppBlazorWeb.Services;

public class DaBase : IDaBase
{
    private readonly string _connectionString = "Data Source=T:/NextCloud/OneDrive/Scripts/Python/JobChecker/jobs.sqlite";

    public async Task<List<JobViewModel>> GetJobsAsync()
    {
        string sqlQuery = @"
            SELECT j.job_id, j.job_board, j.company, j.title, j.apply_url, j.first_seen, j.last_seen, a.applied, a.applied_on
            FROM jobs j
            LEFT JOIN applications a ON j.job_id = a.job_id
            ORDER BY 
                DATE(j.first_seen) DESC,
                MIN(j.first_seen) OVER (PARTITION BY j.company, DATE(j.first_seen)) DESC,
                j.company,
                j.first_seen;";

        return await QueryJobsAsync(sqlQuery).ConfigureAwait(false);
    }

    public async Task<JobViewModel?> UpdateApplicationAsync(ModifyApplicationDto appliedJob)
    {
        await using var conn = new SQLiteConnection(_connectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        string insertQuery = @"
            INSERT INTO applications (job_id, applied, applied_on) 
            VALUES (@job_id, @applied, @applied_on)
            ON CONFLICT(job_id) DO UPDATE SET 
                applied_on = excluded.applied_on,
                applied = excluded.applied;";

        await using (var insertCmd = new SQLiteCommand(insertQuery, conn))
        {
            insertCmd.Parameters.AddWithValue("@job_id", appliedJob.JobId);
            insertCmd.Parameters.AddWithValue("@applied", appliedJob.Applied ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@applied_on", appliedJob.AppliedOn ?? (object)DBNull.Value);
            await insertCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        string selectQuery = @"
            SELECT j.job_id, j.job_board, j.company, j.title, j.apply_url, j.first_seen, j.last_seen, a.applied, a.applied_on
            FROM jobs j
            LEFT JOIN applications a ON j.job_id = a.job_id
            WHERE j.job_id = @job_id;";

        await using var selectCmd = new SQLiteCommand(selectQuery, conn);
        selectCmd.Parameters.AddWithValue("@job_id", appliedJob.JobId);

        await using var reader = await selectCmd.ExecuteReaderAsync().ConfigureAwait(false);
        if (await reader.ReadAsync().ConfigureAwait(false))
        {
            return new JobViewModel
            {
                JobId = reader.GetString(0),
                JobBoard = reader.GetString(1),
                Company = reader.GetString(2),
                Title = reader.GetString(3),
                ApplyUrl = reader.GetString(4),
                FirstSeen = reader.GetDateTime(5),
                LastSeen = reader.GetDateTime(6),
                Applied = reader.IsDBNull(7) ? null : reader.GetString(7),
                AppliedOn = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            };
        }

        return null;
    }

    private async Task<List<JobViewModel>> QueryJobsAsync(string sqlQuery)
    {
        var jobs = new List<JobViewModel>();

        await using var conn = new SQLiteConnection(_connectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        await using var cmd = new SQLiteCommand(sqlQuery, conn);
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            jobs.Add(new JobViewModel
            {
                JobId = reader.GetString(0),
                JobBoard = reader.GetString(1),
                Company = reader.GetString(2),
                Title = reader.GetString(3),
                ApplyUrl = reader.GetString(4),
                FirstSeen = reader.GetDateTime(5),
                LastSeen = reader.GetDateTime(6),
                Applied = reader.IsDBNull(7) ? null : reader.GetString(7),
                AppliedOn = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            });
        }

        return jobs;
    }
}
