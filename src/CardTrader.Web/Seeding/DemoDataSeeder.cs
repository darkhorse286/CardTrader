using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using CardTrader.Identity;
using CardTrader.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Web.Seeding;

public static class DemoDataSeeder
{
    internal const string AdminEmail = "admin@cardtrader.local";
    private const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        await SeedAdminUserAsync(sp);

        var db = sp.GetRequiredService<AppDbContext>();
        if (await db.Cards.AnyAsync()) return;

        db.Cards.AddRange(BuildCards());
        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<CardTraderUser>>();

        if (!await roleManager.RoleExistsAsync(CardTraderRoles.Admin))
            await roleManager.CreateAsync(new IdentityRole(CardTraderRoles.Admin));

        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            admin = new CardTraderUser { UserName = AdminEmail, Email = AdminEmail };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(admin, CardTraderRoles.Admin))
            await userManager.AddToRoleAsync(admin, CardTraderRoles.Admin);
    }

    private static IEnumerable<Card> BuildCards() =>
    [
        // ── Season One Classics ───────────────────────────────────────────────
        Card.Create(CardId.New(),
            name:           "Max \"Shorts\" McGee",
            setName:        "Season One Classics",
            rarity:         "Rare",
            playerName:     "Max \"Shorts\" McGee",
            printRun:       500,
            playerPosition: "Attack",
            teamName:       "Rochester Biscuits"),

        Card.Create(CardId.New(),
            name:           "Tim \"Lucky\" Luciano",
            setName:        "Season One Classics",
            rarity:         "Common",
            playerName:     "Tim \"Lucky\" Luciano",
            printRun:       2000,
            playerPosition: "Midfield",
            teamName:       "Deer Creek Hunters"),

        Card.Create(CardId.New(),
            name:           "Jim \"Cheddar\" McCoy",
            setName:        "Season One Classics",
            rarity:         "Uncommon",
            playerName:     "Jim \"Cheddar\" McCoy",
            printRun:       1000,
            playerPosition: "Defense",
            teamName:       "Red Deer Elves"),

        Card.Create(CardId.New(),
            name:           "Bobby \"Butterfingers\" Barnes",
            setName:        "Season One Classics",
            rarity:         "Common",
            playerName:     "Bobby \"Butterfingers\" Barnes",
            printRun:       1500,
            playerPosition: "Midfield",
            teamName:       "Rochester Biscuits"),

        Card.Create(CardId.New(),
            name:           "Sal \"The Sail\" Salerno",
            setName:        "Season One Classics",
            rarity:         "Legendary",
            playerName:     "Sal \"The Sail\" Salerno",
            printRun:       50,
            playerPosition: "Goalie",
            teamName:       "Millbrook Mudcats"),

        Card.Create(CardId.New(),
            name:           "Fran \"The Fridge\" Frandsen",
            setName:        "Season One Classics",
            rarity:         "Common",
            playerName:     "Fran \"The Fridge\" Frandsen",
            printRun:       2000,
            playerPosition: "Defense",
            teamName:       "Deer Creek Hunters"),

        Card.Create(CardId.New(),
            name:           "Hank \"Hammertoes\" Hoffmann",
            setName:        "Season One Classics",
            rarity:         "Common",
            playerName:     "Hank \"Hammertoes\" Hoffmann",
            printRun:       1800,
            playerPosition: "Defense",
            teamName:       "Gravel Pit Goblins"),

        // ── All-Star Weekend Promos ───────────────────────────────────────────
        Card.Create(CardId.New(),
            name:           "Pete \"Petey Two-Hats\" Dupont",
            setName:        "All-Star Weekend Promos",
            rarity:         "Rare",
            playerName:     "Pete \"Petey Two-Hats\" Dupont",
            printRun:       300,
            playerPosition: "Midfield",
            teamName:       "Gravel Pit Goblins"),

        Card.Create(CardId.New(),
            name:           "Gus \"Noodles\" Nakamura",
            setName:        "All-Star Weekend Promos",
            rarity:         "Ultra Rare",
            playerName:     "Gus \"Noodles\" Nakamura",
            printRun:       100,
            playerPosition: "Attack",
            teamName:       "Sundown Salamanders"),

        Card.Create(CardId.New(),
            name:           "Walt \"Waffles\" Winterbottom",
            setName:        "All-Star Weekend Promos",
            rarity:         "Uncommon",
            playerName:     "Walt \"Waffles\" Winterbottom",
            printRun:       750,
            playerPosition: "Attack",
            teamName:       "Pinecrest Platypuses"),

        Card.Create(CardId.New(),
            name:           "Dot \"Dynamite\" Doherty",
            setName:        "All-Star Weekend Promos",
            rarity:         "Rare",
            playerName:     "Dot \"Dynamite\" Doherty",
            printRun:       400,
            playerPosition: "Goalie",
            teamName:       "Millbrook Mudcats"),

        Card.Create(CardId.New(),
            name:           "Carlos \"Sparks\" Delgado",
            setName:        "All-Star Weekend Promos",
            rarity:         "Ultra Rare",
            playerName:     "Carlos \"Sparks\" Delgado",
            printRun:       150,
            playerPosition: "Attack",
            teamName:       "Red Deer Elves"),
    ];
}
