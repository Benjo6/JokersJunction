﻿namespace JokersJunction.Shared.Models
{
    public class CreateNoteResult
    {
        public bool Successful { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
