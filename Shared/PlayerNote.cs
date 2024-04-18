﻿namespace JokersJunction.Shared
{
    public class PlayerNote
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public string NotedPlayerName { get; set; }

        public string Description { get; set; }

        public ApplicationUser ApplicationUser { get; set; }
    }
}
