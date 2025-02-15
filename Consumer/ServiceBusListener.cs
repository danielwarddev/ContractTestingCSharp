using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;

namespace Consumer;

public record Employee(int Id, string Name, string Department, string Position);

public class ServiceBusListener
{
    private readonly IEmployeeService _employeeService;

    public ServiceBusListener(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }
    
    [FunctionName("MyFunction")]
    public async Task Run(
        [ServiceBusTrigger("employee-queue", Connection = "ServiceBusConnectionString")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var bodyString = message.Body.ToString();
        var employee = JsonSerializer.Deserialize<Employee>(bodyString)!;

        await _employeeService.ProcessEmployee(employee);
        
        await Task.CompletedTask;
    }
}