﻿namespace MCTG;

public class Trade
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public int UserId { get; set; }
    public string RequiredType { get; set; }
    public int MinimumDamage { get; set; }
}