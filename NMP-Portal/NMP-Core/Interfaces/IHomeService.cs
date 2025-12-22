namespace NMP.Core.Interfaces;
public interface IHomeService
{
    Task<bool> IsDefraCustomerIdentifyConfigurationWorkingAsync();
    Task<bool> IsNmptServiceWorkingAsync();
}
