namespace NMP.Application;
public interface IAboutServiceLogic
{
    Task<bool> UpdateShowAboutAsync(bool doNotShowAboutThisService);
    Task<bool> CheckDoNotShowAboutThisService();
}
