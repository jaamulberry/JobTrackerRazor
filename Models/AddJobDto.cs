namespace JobAppRazorWeb.Models;

public class AddJobDto
{
    public string JobBoard { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ApplyUrl { get; set; } = string.Empty;
}
