using LineBotMessage.Domain;
using LineBotMessage.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------
// 加入 Serilog 設定 (請加在 builder.Services.AddControllers() 之前)
// -----------------------------------------------------------------
// 告訴 Host 使用 Serilog，並自動讀取 appsettings.json 的設定
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 配置 HttpClient 依賴注入
builder.Services.AddHttpClient();

// -----------------------------------------------------------------
// 註冊您的服務到 DI 容器
// AddScoped: 這是 Web API 最推薦的生命週期，
// 代表在同一個 HTTP 請求中，只會共用一個實例。
builder.Services.AddScoped<LineBotService>();
builder.Services.AddScoped<RichMenuService>();
builder.Services.AddScoped<ServerCheckService>();
// -----------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}
// 自動記錄每一個 HTTP 請求的詳細資訊 (耗時、狀態碼等)
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
