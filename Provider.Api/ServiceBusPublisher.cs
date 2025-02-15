using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Provider.Api;

public record Employee(int Id, string Name, string Department, string Position);

public class ServiceBusPublisher
{
    private ServiceBusSender _sender;
    
    public ServiceBusPublisher(ServiceBusClient client)
    {
        _sender = client.CreateSender("employee-queue");
    }

    public async Task PublishEmployee(Employee employee)
    {
        var message = new ServiceBusMessage(JsonSerializer.Serialize(employee))
        {
            ContentType = "application/json",
        };
        await _sender.SendMessageAsync(message);
    }
}

// Presumably, there would be some more complicated logic in here in a real app
 // For instance, reading from a database or calculating some of the values
 // You would then use this class to create an Employee and pass it to the ServiceBusPublisher
 // Because this is how we create our business object, this is what we contract test
 public interface IEmployeeGenerator
 {
     Employee CreateEmployee();
 }
 public class EmployeeGenerator : IEmployeeGenerator
 {
     public Employee CreateEmployee()
     {
         return new Employee(1, "John Doe", "Sales", "Sales Manager");
     }
 }