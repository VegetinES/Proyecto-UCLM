using System;
using Realms;

public class UserLogin : RealmObject
{
    [PrimaryKey]
    public int ID { get; set; }
    
    public User User { get; set; }
    
    public DateTimeOffset LoginDate { get; set; }
}