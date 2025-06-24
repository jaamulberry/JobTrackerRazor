using JobAppRazorWeb.Models;
using JobAppRazorWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JobAppRazorWeb.Pages;

public class JobsModel(IDaBase daBase) : PageModel
{
    public JobTrackerViewModel JobTracker { get; set; } = new();
    [BindProperty(SupportsGet = true)] public bool ShowApplied { get; set; } = true;
    [BindProperty(SupportsGet = true)] public bool HideNo { get; set; } = true;
    [BindProperty(SupportsGet = true)] public string? CompanySearch { get; set; }
    [BindProperty(SupportsGet = true)] public string? UrlSearch { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? ShowDates { get; set; }

    public async Task OnGet()
    {
        var rawJobs = await daBase.GetJobsAsync();

        var indexedJobs = rawJobs.Select((job, index) =>
        {
            job.JobIndex = index + 1;
            return job;
        }).ToList();
        
        //Filter out Company Search and URL Search results
        var filteredJobs = indexedJobs
            .Where(j => string.IsNullOrEmpty(CompanySearch) ||
                        j.Company.Contains(CompanySearch, StringComparison.OrdinalIgnoreCase))
            .Where(j => string.IsNullOrEmpty(UrlSearch) ||
                        UrlMatch(j.ApplyUrl))
            .Where(j => ShowApplied || j.Applied != "YES")
            .Where(j => !HideNo || j.Applied != "NO" || 
                        (!string.IsNullOrEmpty(CompanySearch) &&
                         j.Company.Contains(CompanySearch, StringComparison.OrdinalIgnoreCase)))
            .Where(j => j.Applied != "DUPE")
            .ToList();

        int newJobCount = indexedJobs.Count(j => j.FirstSeen.Date == DateTime.Today);

        int newJobAvailableCount = indexedJobs.Count(j => j.FirstSeen.Date == DateTime.Today && j.Applied == null);
        
        int appliedTodayCount = indexedJobs.Count(j => j.AppliedOn?.Date == DateTime.Today);

        JobTracker = new JobTrackerViewModel
        {
            Jobs = filteredJobs,
            NewJobCount = newJobCount,
            NewJobAvailableCount = newJobAvailableCount,
            AppliedTodayCount = appliedTodayCount
        };
    }

    public async Task<IActionResult> OnPostModifyApplicationAsync([FromBody] ModifyApplicationDto modifiedApplication)
    {
        var updatedJob = await daBase.UpdateApplicationAsync(modifiedApplication);
        if (updatedJob == null)
        {
            return BadRequest();
        }
        return new JsonResult(updatedJob.JobId);
    }

    public async Task<IActionResult> OnPostAddJobAsync([FromBody] AddJobDto newJob)
    {
        var addedJob = await daBase.AddJobAsync(newJob);
        if (addedJob == null)
        {
            return BadRequest();
        }
        return new JsonResult(addedJob.JobId);
    }
    
    private bool UrlMatch(string url)
    {
        string[] allTerms = (UrlSearch?.Split("OR") ?? Array.Empty<string>())
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        string[] includeTerms = allTerms.Where(t => !t.StartsWith("!")).ToArray();
        string[] excludeTerms = allTerms.Where(t => t.StartsWith("!")).Select(t => t.Substring(1)).ToArray();

        bool containsInclude = includeTerms.Any(t => url.Contains(t, StringComparison.OrdinalIgnoreCase));
        bool containsExclude = excludeTerms.Any(t => url.Contains(t, StringComparison.OrdinalIgnoreCase));

        if (containsExclude)
        {
            return false;
        }

        return includeTerms.Length == 0 || containsInclude;
    }
}