using AIAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<LlmSettingsService>();

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);


builder.Services.AddHttpClient();

builder.Services.AddScoped<ITestAnalysisService, TestAnalysisService>();
builder.Services.AddScoped<IReportService, ReportService>();           

builder.Services.AddScoped<IAiService, AiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();