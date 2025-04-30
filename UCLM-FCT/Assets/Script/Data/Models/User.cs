using System;
using System.Linq;
using Realms;

public class User : RealmObject
{
    [PrimaryKey]
    public string UID { get; set; }
    
    public string Email { get; set; }
    
    public bool IsTutor { get; set; }
    
    public Configuration Configuration { get; set; }
    
    public ParentalControl ParentalControl { get; set; }
    
    [Backlink(nameof(Profile.User))]
    public IQueryable<Profile> Profiles { get; }
    
    public DateTimeOffset CreationDate { get; set; }
    
    public DateTimeOffset LastLogin { get; set; }
    
    [Backlink(nameof(UserLogin.User))]
    public IQueryable<UserLogin> Logins { get; }
}