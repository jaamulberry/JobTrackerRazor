namespace JobAppBlazorWeb.Models;

public class JobViewModel
{
    public int JobIndex { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string JobBoard { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ApplyUrl { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public string? Applied { get; set; }
    public DateTime? AppliedOn { get; set; }
}