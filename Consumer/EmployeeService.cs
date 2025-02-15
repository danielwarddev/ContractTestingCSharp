namespace Consumer;

public interface IEmployeeService
{
    Task ProcessEmployee(Employee employee);
}

public class EmployeeService : IEmployeeService
{
    private readonly EmployeeRepository _repository;

    public EmployeeService(EmployeeRepository repository)
    {
        _repository = repository;
    }
    
    public async Task ProcessEmployee(Employee employee)
    {
        await _repository.ProcessEmployee(employee);
    }
}

public class EmployeeRepository
{
    public async Task ProcessEmployee(Employee employee)
    {
        await Task.CompletedTask;
    }
}