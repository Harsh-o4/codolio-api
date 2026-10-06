using Codolio.Services;

var builder = WebApplication.CreateBuilder(args);

//Add service to the container
builder.Services.AddControllers();

//Register Scraper service
builder.Services.AddHttpClient<IScraperService, ScraperService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
});

//Configure CORS for frontend Access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

//configure swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//Enable swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json","Codolio Live Profile Scraper API v1");
    c.RoutePrefix = String.Empty;
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
