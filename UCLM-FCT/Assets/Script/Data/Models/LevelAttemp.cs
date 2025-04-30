using System;
using Realms;

public class LevelAttempt : RealmObject
{
    [PrimaryKey]
    public int AttemptID { get; set; }

    public Statistics Statistics { get; set; }

    public bool Completed { get; set; }
    public bool HelpUsed { get; set; }
    public int TimeSpent { get; set; }
    public DateTimeOffset CompletionDate { get; set; }
}