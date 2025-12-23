namespace NMP.Application;
public interface IHomeLogic
{
    Task<bool> IsDefraCustomerIdentifyConfigurationWorkingAsync();
    Task<bool> IsNmptServiceWorkingAsync();
}
