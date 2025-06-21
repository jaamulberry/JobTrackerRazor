namespace JobAppBlazorWeb.Models;

public class JobTrackerViewModel
{
    public List<JobViewModel> Jobs { get; set; } = [];
    public int NewJobCount { get; set; }
    public int NewJobAvailableCount { get; set; }
    public int AppliedTodayCount { get; set; }
}