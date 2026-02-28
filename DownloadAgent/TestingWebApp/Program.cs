var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/textfile", (int? size = null) =>
{
    System.Threading.Thread.Sleep(1000);
    var targetSize = size ?? 100; // Default to 100 bytes if not specified
    var textContent = GenerateTextContent(targetSize);
    return Results.Text(textContent, "text/plain");
})
.WithName("GetTextFile");

string GenerateTextContent(int sizeInBytes)
{
    const string baseText = "Hello, this is a text file!\n";
    var content = new System.Text.StringBuilder();
    
    while (content.Length < sizeInBytes)
    {
        var remaining = sizeInBytes - content.Length;
        if (remaining < baseText.Length)
        {
            content.Append(baseText.Substring(0, remaining));
            break;
        }
        content.Append(baseText);
    }
    
    return content.ToString();
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
