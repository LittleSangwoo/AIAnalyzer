using AIAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<LlmSettingsService>();

// Подключаем провайдер кодировок для поддержки русского Excel (Windows-1251)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

// 1. Добавляем HttpClient (нужна для ИИ-запросов)
builder.Services.AddHttpClient();

// Регистрация сервисов
builder.Services.AddScoped<ITestAnalysisService, TestAnalysisService>(); 
builder.Services.AddScoped<IReportService, ReportService>();             

// 2. Регистрация ИИ-сервиса
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