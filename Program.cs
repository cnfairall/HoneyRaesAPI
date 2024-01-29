using HoneyRaesAPI.Models;

List<Customer> customers = new List<Customer>
{
    new Customer
    {
        Id = 0,
        Name = "John Candy",
        Address = "64 White Way, Decatur, GA 20006"
    },
    new Customer
    {
        Id = 1,
        Name = "Bill Cunningham",
        Address = "742 Evergreen Terrace, Springfield, MA 10011"
    },
    new Customer
    {
        Id = 2,
        Name = "Walter White",
        Address = "1800 Pennsylvania Blvd, Washington, D.C. 70095"
    }
};
List<Employee> employees = new List<Employee>
{
    new Employee
    {
        Id = 0,
        Name = "Lady Gaga",
        Specialty = "Mechanics"
    },
    new Employee
    {
        Id = 1,
        Name = "Beyonce",
        Specialty = "Electrical"
    },
    new Employee
    {
        Id = 2,
        Name = "Dean Martin",
        Specialty = "Construction"
    }
};

List<ServiceTicket> serviceTickets = new List<ServiceTicket>
{
    new ServiceTicket
    {
        Id = 0,
        CustomerId = 0,
        Description = "Organ failure",
        Emergency = false,
    },
    new ServiceTicket
    {
        Id = 1,
        CustomerId = 1,
        Description = "Misappropriation",
        Emergency = true,
    },
    new ServiceTicket
    {
        Id = 2,
        CustomerId = 1,
        EmployeeId = 2,
        Description = "Down bad",
        Emergency = true,
    },
    new ServiceTicket
    {
        Id = 3,
        EmployeeId= 2,
        CustomerId = 2,
        Description = "Complete insanity",
        Emergency = false,
    },
    new ServiceTicket
    {
        Id = 4,
        CustomerId = 2,
        Description = "Reverse optimism",
        Emergency = true,
        DateCompleted = DateTime.Today

    }
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/servicetickets", () =>
{
    return serviceTickets;
});

app.MapGet("/serviceTickets/{id}", (int id) =>
{
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (serviceTicket == null)
    {
        return Results.NotFound();
    }
    serviceTicket.Customer = customers.FirstOrDefault(c => c.Id == serviceTicket.CustomerId);
    serviceTicket.Employee = employees.FirstOrDefault(e => e.Id == serviceTicket.EmployeeId);
    return Results.Ok(serviceTicket);
});

app.MapGet("/customers", () =>
{
    return customers;
});

app.MapGet("/customers/{id}", (int id) =>
{
    Customer customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    customer.ServiceTickets = serviceTickets.Where(st => st.CustomerId == id).ToList();
    return Results.Ok(customer);
});

app.MapGet("/employees", () =>
{
    return employees;
});

app.MapGet("/employees/{id}", (int id) =>
{
    Employee employee = employees.FirstOrDefault(e => e.Id == id);
    if (employee == null)
    {
        return Results.NotFound();
    }
    employee.ServiceTickets = serviceTickets.Where(st => st.EmployeeId == id).ToList();
    return Results.Ok(employee);
});

app.MapPost("/serviceTickets", (ServiceTicket serviceTicket) =>
{
    serviceTicket.Id = serviceTickets.Max(st => st.Id) + 1;
    serviceTickets.Add(serviceTicket);
    return serviceTicket;
});

app.MapDelete("/serviceTickets/{id}", (int id) =>
{
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (serviceTicket == null)
    {
        return Results.NotFound();
    }
    serviceTickets.Remove(serviceTicket);
    return Results.Ok();
});

//update a ticket
app.MapPut("/serviceTickets/{id}", (int id, ServiceTicket serviceTicket) =>
{
    ServiceTicket ticketToUpdate = serviceTickets.FirstOrDefault(st => st.Id == id);
    int ticketIndex = serviceTickets.IndexOf(ticketToUpdate);

    if (ticketToUpdate == null)
    {
        return Results.NotFound();
    }
    if (id != ticketToUpdate.Id)
    {
        return Results.BadRequest();
    }
    serviceTickets[ticketIndex] = serviceTicket;
    return Results.Ok();
});

//complete a ticket
app.MapPost("/servicetickets/{id}/complete", (int id) =>
{
    ServiceTicket ticketToComplete = serviceTickets.FirstOrDefault(st => st.Id == id);
    ticketToComplete.DateCompleted = DateTime.Today;
});

//get emergencies
app.MapGet("/serviceTickets/emergency", () =>
{
    List<ServiceTicket> emergentTickets = serviceTickets.Where(st => st.Emergency == true && st.DateCompleted == new DateTime()).ToList();
    return emergentTickets;
});

//get unassigned
app.MapGet("/serviceTickets/unassigned", () =>
{
    List<ServiceTicket> unassignedTickets = serviceTickets.Where(st => st.EmployeeId == null).ToList();
    return unassignedTickets;
});

//get inactive
app.MapGet("/customers/inactive", () =>
{
    DateTime yearAgo = DateTime.Today.AddDays(-365);
    List<ServiceTicket> oldTickets = serviceTickets.Where(st => st.DateCompleted < yearAgo && st.DateCompleted > new DateTime()).ToList();
    if (oldTickets == null)
    {
        return Results.NotFound();
    }

    foreach (ServiceTicket ticket in oldTickets)
    {
        ticket.Customer = customers.FirstOrDefault(c => c.Id == ticket.CustomerId);
    }

    List<Customer> inactiveCustomers = oldTickets.Select(t => t.Customer).ToList();
    return Results.Ok(inactiveCustomers);
});

//get available employees
app.MapGet("/employees/available", () =>
{
    List<Employee> availableEmployees = new List<Employee>();
    List<Employee> assignedEmployees = new List<Employee>();
    List<ServiceTicket> openTickets = new List<ServiceTicket>();

    openTickets = serviceTickets.Where(st => st.DateCompleted == null).ToList();

    foreach (ServiceTicket ticket in openTickets)
    {
        var assignedEmployee = employees.FirstOrDefault(e => e.Id == ticket.EmployeeId);
        assignedEmployees.Add(assignedEmployee);
    }

    availableEmployees = employees.Where(e => !assignedEmployees.Contains(e)).ToList();

    return availableEmployees;

});

//get one employee's customers
app.MapGet("/employees/{id}/customers", (int id) =>
{
    List<Customer> customersList = new();
    List<ServiceTicket> employeesTickets = serviceTickets.Where(st => st.EmployeeId == id).ToList();
    
    foreach (ServiceTicket ticket in employeesTickets)
    {
        Customer employeesCustomer = customers.FirstOrDefault(c => c.Id == ticket.CustomerId);
        customersList.Add(employeesCustomer);
    }

    return Results.Ok(customersList);
    
});

//get employee with most completed tickets
app.MapGet("employees/EOTM", () =>
{
    List<ServiceTicket> closedTickets = serviceTickets.Where(st => st.DateCompleted != null).ToList();

    List<Employee> mostTickets = employees.OrderByDescending(e => closedTickets.Count(t => t.EmployeeId == e.Id)).ToList();

    Employee employeeOTM = mostTickets.First();

    return employeeOTM;
});

//get oldest tickets
app.MapGet("serviceTickets/completed/oldest", () =>
{
    List<ServiceTicket> closedTicketsByAge = serviceTickets.Where(st => st.DateCompleted != null).OrderBy(st => st.DateCompleted).ToList();
    return closedTicketsByAge;
});

//get priority tickets
app.MapGet("serviceTickets/priority", () =>
{
    List<ServiceTicket> openTickets = serviceTickets.Where(st => st.DateCompleted == null).ToList();
    List<ServiceTicket> priorityTickets = openTickets.OrderByDescending(t => t.Emergency == true).ThenByDescending(t => t.EmployeeId == null).ToList();

    return priorityTickets;
});

app.Run();
