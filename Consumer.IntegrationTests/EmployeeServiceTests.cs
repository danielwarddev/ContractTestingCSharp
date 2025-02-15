using PactNet;
using Match = PactNet.Matchers.Match;

namespace Consumer.IntegrationTests;

public class EmployeeServiceTests
{
    private readonly EmployeeService _employeeService =
        new EmployeeService(new EmployeeRepository());
    private readonly IMessagePactBuilderV4 _pactBuilder = Pact
        .V4("Employee Message Consumer", "Employee Message Publisher", new PactConfig())
        .WithMessageInteractions();
 
    [Fact]
    public async Task Inserts_Employee_Into_Database()
    {
        var expectedEmployeeEvent = new Employee(1, "John Doe", "Sales", "Sales Manager");

        await _pactBuilder
            .ExpectsToReceive("An employee event")
            .WithMetadata("contentType", "application/json")
            .WithJsonContent(Match.Type(expectedEmployeeEvent))
            .VerifyAsync<Employee>(async employeeEvent =>
            {
                // Notice we don't use the ServiceBusListener (the adapter) here for the contract test
                // Instead, we use the EmployeeService (the port), which cares about the actual business object
                await _employeeService.ProcessEmployee(employeeEvent);
                
                // In a real app, you'd also have an assertion here
                // eg. checking if the employee got inserted into the database for a Created event
                // For simplicity's sake, this is not shown here
            });
    }

    [Fact]
    public async Task Inserts_Employee_Into_Database_With_State()
    {
        var expectedEmployeeEvent = new Employee(1, "John Doe", "Sales", "Sales Manager");

        await _pactBuilder
            .ExpectsToReceive("An employee event")
            // Given() is probably more rarely needed for message contracts
            // You're not expecting multiple status codes, it either goes on the queue or it doesn't
            .Given("some expected provider state")
            .WithMetadata("contentType", "application/json")
            .WithJsonContent(Match.Type(expectedEmployeeEvent))
            .VerifyAsync<Employee>(async employeeEvent =>
            {
                await _employeeService.ProcessEmployee(employeeEvent);
                // Some assert statement
            });
    }
}