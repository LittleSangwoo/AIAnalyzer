using AIAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Добавляем фабрику HttpClient (нужна для твоих ИИ-запросов)
builder.Services.AddHttpClient();

// Регистрация сервисов
builder.Services.AddScoped<ITestAnalysisService, TestAnalysisService>(); // Сервис напарницы
builder.Services.AddScoped<IReportService, ReportService>();             // Твой сервис Excel

// 2. ДОБАВЬ ЭТУ СТРОЧКУ: Регистрация твоего ИИ-сервиса
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