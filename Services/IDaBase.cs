using JobAppBlazorWeb.Models;

namespace JobAppBlazorWeb.Services;

public interface IDaBase
{
    public Task<List<JobViewModel>> GetJobsAsync();
    public Task<JobViewModel?> UpdateApplicationAsync(ModifyApplicationDto appliedJob);
}