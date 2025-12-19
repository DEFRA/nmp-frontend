namespace NMP.Application;
public interface IAboutServiceLogic
{
    Task<bool> UpdateShowAboutServiceAsync(bool doNotShowAboutThisService);
    Task<bool> CheckDoNotShowAboutThisService();
}
