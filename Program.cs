using System.Collections.Immutable;
using JobAppRazorWeb.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Remove anything that might register file-based DP / key management
builder.Services.RemoveAll<IDataProtectionProvider>();
builder.Services.RemoveAll<IConfigureOptions<KeyManagementOptions>>();

// Force ephemeral provider only (no files, no encryptor warnings)
builder.Services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IDaBase, DaBase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.UseStaticFiles();
// Allow serving files from wwwroot even if not in manifest
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name == "_contenthash.txt")
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, ncmdo-store, must-revalidate";
        }
    }
});


app.MapRazorPages()
    .WithStaticAssets();

app.Run();