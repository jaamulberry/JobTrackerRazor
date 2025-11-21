using JobAppRazorWeb.Models;
using System.Security.Cryptography;
using System.Text;
using Npgsql;


namespace JobAppRazorWeb.Services;

public class DaBase : IDaBase
{
    private readonly string _connectionString;

public DaBase(IConfiguration config)
{
    Console.WriteLine("========== DaBase START ==========");

    // Dump env var PG_CONNECTION_STRING
    var envConn = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING");
    Console.WriteLine($"[DaBase DEBUG] PG_CONNECTION_STRING null/empty? " + (string.IsNullOrWhiteSpace(envConn) ? "YES" : "NO"));

    // Dump DefaultConnection from config
    var cfgConn = config.GetConnectionString("DefaultConnection");

    // Decide which one to use
    string? connectionString = !string.IsNullOrWhiteSpace(envConn)
        ? envConn
        : cfgConn;

    Console.WriteLine($"[DaBase DEBUG] Final connection string null/empty? " +
                      (string.IsNullOrWhiteSpace(connectionString) ? "YES" : "NO"));

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("[DaBase DEBUG] No usable connection string. Throwing.");
        Console.WriteLine("========== DaBase END (FAIL) ==========");
        throw new InvalidOperationException("Connection String Not Found");
    }

    _connectionString = connectionString;

    Console.WriteLine($"[DaBase DEBUG] Using connection string. Found valid one.");
    Console.WriteLine("========== DaBase END (OK) ==========");
}



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
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        string insertQuery = @"
            INSERT INTO applications (job_id, applied, applied_on) 
            VALUES (@job_id, @applied, @applied_on)
            ON CONFLICT(job_id) DO UPDATE SET 
                applied_on = excluded.applied_on,
                applied = excluded.applied;";

        await using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
        {
            insertCmd.Parameters.AddWithValue("@job_id", appliedJob.JobId);
            insertCmd.Parameters.AddWithValue("@applied", appliedJob.Applied);
            insertCmd.Parameters.AddWithValue("@applied_on", appliedJob.AppliedOn ?? (object)DBNull.Value);
            await insertCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        string selectQuery = @"
            SELECT j.job_id, j.job_board, j.company, j.title, j.apply_url, j.first_seen, j.last_seen, a.applied, a.applied_on
            FROM jobs j
            LEFT JOIN applications a ON j.job_id = a.job_id
            WHERE j.job_id = @job_id;";

        await using var selectCmd = new NpgsqlCommand(selectQuery, conn);
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

    public async Task<JobViewModel?> AddJobAsync(AddJobDto newJob)
    {
        string jobId = HashUrl(newJob.ApplyUrl);
        var now = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ");
        string manualJobBoard = newJob.JobBoard + " - Manual";
        
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        string insertJob = @"
            INSERT INTO jobs (job_id, job_board, company, title, apply_url, first_seen, last_seen)
            VALUES (@job_id, @job_board, @company, @title, @apply_url, @now, @now)
            ON CONFLICT(job_id) DO UPDATE SET
                last_seen = excluded.last_seen;";

        await using (var jobCmd = new NpgsqlCommand(insertJob, conn))
        {
            jobCmd.Parameters.AddWithValue("@job_id", jobId);
            jobCmd.Parameters.AddWithValue("@job_board", manualJobBoard);
            jobCmd.Parameters.AddWithValue("@company", newJob.Company);
            jobCmd.Parameters.AddWithValue("@title", newJob.Title);
            jobCmd.Parameters.AddWithValue("@apply_url", newJob.ApplyUrl);
            jobCmd.Parameters.AddWithValue("@now", now);
            await jobCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        string insertApp = @"
            INSERT INTO applications (job_id, applied, applied_on)
            VALUES (@job_id, 'YES', @now)
            ON CONFLICT(job_id) DO UPDATE SET
                applied = excluded.applied,
                applied_on = excluded.applied_on;";

        await using (var appCmd = new NpgsqlCommand(insertApp, conn))
        {
            appCmd.Parameters.AddWithValue("@job_id", jobId);
            appCmd.Parameters.AddWithValue("@now", now);
            await appCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        string selectQuery = @"
            SELECT j.job_id, j.job_board, j.company, j.title, j.apply_url, j.first_seen, j.last_seen, a.applied, a.applied_on
            FROM jobs j
            LEFT JOIN applications a ON j.job_id = a.job_id
            WHERE j.job_id = @job_id;";

        await using var selectCmd = new NpgsqlCommand(selectQuery, conn);
        selectCmd.Parameters.AddWithValue("@job_id", jobId);

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

    private static string HashUrl(string url)
    {
        using var md5 = MD5.Create();
        byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    private async Task<List<JobViewModel>> QueryJobsAsync(string sqlQuery)
    {
        var jobs = new List<JobViewModel>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(sqlQuery, conn);
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
