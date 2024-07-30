using System.Text.Json.Serialization;
using HoneyRaesAPI.Models;
using Microsoft.AspNetCore.Mvc.Formatters;

List<Customer> customers = new List<Customer>
{
    new() { Id = 1, Name = "Canton Thurgood", Address = "541 Pine Grass Lane"},
    new() { Id = 2, Name = "Georgina Florentine", Address = "3 Nobility Drive"},
    new() { Id = 3, Name = "Candelabra Muddlebuster", Address = "10938 Hwy 40 W"}
};
List<Employee> employees = new List<Employee>
{
    new() { Id = 1, Name = "Split", Specialty = "Motor Refurb"},
    new() { Id = 2, Name = "Hannah Marshall", Specialty = "Window Repair"},
    new() { Id = 3, Name = "Colonel Mustard", Specialty = "Pugnacious Litigation"}
};
List<ServiceTicket> serviceTickets = new List<ServiceTicket>
{
    new() { Id = 1, CustomerId = 3, EmployeeId = 1, Description = "Timing Belt Replacement", Emergency = false, DateCompleted = new DateTime(2021, 12, 12) },
    new() { Id = 2, CustomerId = 2, Description = "Pistons Seized", Emergency = true },
    new() { Id = 3, CustomerId = 3, Description = "Torn Rotator Cuff", Emergency = false },
    new() { Id = 4, CustomerId = 1, EmployeeId = 2, Description = "Chipped Windscreen", Emergency = false },
    new() { Id = 5, CustomerId = 1, EmployeeId = 3, Description = "Chipped Rearscreen", Emergency = false },
    new() { Id = 6, CustomerId = 1, EmployeeId = 2, Description = "Attempted Vehicle Theft with Full Glass Loss", Emergency = true },
    new() { Id = 7, CustomerId = 1, Description = "Minor Graffiti", Emergency = true }
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
  options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/servicetickets", () =>
{
    return serviceTickets;
});

app.MapGet("/servicetickets/{id}", (int id) =>
{
    ServiceTicket? serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (serviceTicket == null)
    {
        return Results.NotFound();
    }
    serviceTicket.Employee = employees.FirstOrDefault(e => e.Id == serviceTicket.EmployeeId);
    serviceTicket.Customer = customers.FirstOrDefault(c => c.Id == serviceTicket.CustomerId);
    return Results.Ok(serviceTicket);
});

app.MapGet("/servicetickets/openemergencies", () =>
{
    return serviceTickets
        .Where(st => st.DateCompleted == null)
        .Where(st => st.Emergency == true)
        .ToList();
});

app.MapGet("/servicetickets/prioritized", () =>
{
    return serviceTickets
        .Where(st => st.DateCompleted == null)
        .OrderByDescending(st => st.Emergency)
        .ThenBy(st => st.EmployeeId != null)
        .ToList();
});

app.MapGet("/servicetickets/unassigned", () =>
{
    return serviceTickets
        .Where(st => st.EmployeeId == null)
        .ToList();
});

app.MapGet("/servicetickets/past", () =>
{
    return serviceTickets
            .Where(st => st.DateCompleted != null)
            .OrderBy(st => st.DateCompleted)
            .ToList();
});

app.MapPost("/servicetickets", (ServiceTicket serviceTicket) =>
{
    serviceTicket.Id = serviceTickets.Max(st => st.Id) + 1;
    serviceTickets.Add(serviceTicket);
    return serviceTicket;
});

app.MapPatch("/servicetickets/{id}/complete", (int id) =>
{
    ServiceTicket? serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (serviceTicket == null)
    {
        return Results.NotFound();
    };
    if (serviceTicket.DateCompleted == null)
    {
        serviceTicket.DateCompleted = DateTime.Today;
    }
    return Results.Ok(serviceTicket);
});

app.MapPut("/servicetickets/{id}", (int id, ServiceTicket serviceTicket) =>
{
    ServiceTicket? ticketToUpdate = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (ticketToUpdate == null)
    {
        return Results.NotFound();
    }
    //the id in the request route doesn't match the id from the ticket in the request body. That's a bad request!
    if (id != serviceTicket.Id)
    {
        return Results.BadRequest();
    }
    int ticketIndex = serviceTickets.IndexOf(ticketToUpdate);
    serviceTickets[ticketIndex] = serviceTicket;
    return Results.Ok();
});

app.MapDelete("/servicetickets/{id}", (int id) =>
{
    serviceTickets.RemoveAll(st => st.Id == id);
});

app.MapGet("/employees", () =>
{
    return employees;
});

app.MapGet("/employees/available", () =>
{
    return employees
        .Where(e => !serviceTickets.Any(st => st.EmployeeId == e.Id && st.DateCompleted == null))
        .ToList();
});

app.MapGet("/employees/{id}", (int id) =>
{
    Employee? employee = employees.FirstOrDefault(e => e.Id == id);
    if (employee == null)
    {
        return Results.NotFound();
    };
    employee.ServiceTickets = serviceTickets.Where(st => st.EmployeeId == id).ToList();
    return Results.Ok(employee);
});

app.MapGet("/employees/{id}/customers", (int id) =>
{
    if (employees.FirstOrDefault(e => e.Id == id) == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(customers
        .Where(c => serviceTickets.Any(st => st.CustomerId == c.Id && st.EmployeeId == id))
        .ToList());
});

app.MapGet("/employees/of-the-month", () =>
{
    DateTime lastMonth = DateTime.Today.AddMonths(-1);
    return employees
        .OrderByDescending(e => serviceTickets
                        .Where(st => st.EmployeeId == e.Id)
                        .Where(st => st.DateCompleted?.Year == lastMonth.Year && st.DateCompleted?.Month == lastMonth.Month)
                        .Count())
        .First();
});

app.MapGet("/customers", () =>
{
    return customers;
});

app.MapGet("/customers/{id}", (int id) =>
{
    Customer? customer = customers.FirstOrDefault(e => e.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    customer.ServiceTickets = serviceTickets.Where(st => st.CustomerId == id).ToList();
    return Results.Ok(customer);
});

app.MapGet("/customers/inactive", () =>
{
    TimeSpan pastYear = DateTime.Now - DateTime.Now.AddYears(-1);
    return customers
        .Where(c => DateTime.Today - (serviceTickets.Where(st => st.CustomerId == c.Id).Max(c => c.DateCompleted) ?? DateTime.MinValue) > pastYear)
    .ToList();
});

app.Run();
