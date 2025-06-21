namespace JobAppRazorWeb.Models;

public class ModifyApplicationDto
{
    public string JobId { get; set; } = string.Empty;
    public string Applied {get; set;} = string.Empty;
    public DateTime? AppliedOn {get; set;}
}