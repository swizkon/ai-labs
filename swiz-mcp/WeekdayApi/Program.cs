var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/weekday/{date}", (DateOnly date) =>
{
    var weekday = date.DayOfWeek.ToString();
    return Results.Ok(new WeekdayResponse(date, weekday));
})
.WithName("GetWeekday")
.WithDescription("Returns the weekday for a given date (format: yyyy-MM-dd).");

app.Run();

record WeekdayResponse(DateOnly Date, string Weekday);
