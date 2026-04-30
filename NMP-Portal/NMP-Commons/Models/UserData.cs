namespace NMP.Commons.Models;
public class UserData
{
    public UserData()
    {
        User = new User();
        Organisation = new Organisation();
    }
    public User User { get; set; }
    public Organisation Organisation { get; set; }
}
