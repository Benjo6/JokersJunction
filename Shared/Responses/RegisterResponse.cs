﻿namespace JokersJunction.Shared.Responses;

public class RegisterResponse
{
    public bool Successful { get; set; }
    public IEnumerable<string> Errors { get; set; }
}