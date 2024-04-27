using Microsoft.AspNetCore.Identity;

namespace JokersJunction.Shared.Models;

public class ApplicationUser : IdentityUser
{
    public int Currency { get; set; }

    public ICollection<PlayerNote> PlayerNotes { get; set; }
}