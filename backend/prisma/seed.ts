import { PrismaClient } from '@prisma/client';
import bcrypt from 'bcryptjs';

const prisma = new PrismaClient();

async function main() {
  console.log('Seeding database...');

  // Create ranks
  const ranks = [
    { id: 1, name: 'Bronze', minRating: 0, maxRating: 999, icon: 'bronze', color: '#CD7F32' },
    { id: 2, name: 'Silver', minRating: 1000, maxRating: 1499, icon: 'silver', color: '#C0C0C0' },
    { id: 3, name: 'Gold', minRating: 1500, maxRating: 1999, icon: 'gold', color: '#FFD700' },
    { id: 4, name: 'Platinum', minRating: 2000, maxRating: 2499, icon: 'platinum', color: '#E5E4E2' },
    { id: 5, name: 'Diamond', minRating: 2500, maxRating: 2999, icon: 'diamond', color: '#B9F2FF' },
    { id: 6, name: 'Master', minRating: 3000, maxRating: 3999, icon: 'master', color: '#9D4EDD' },
    { id: 7, name: 'Grandmaster', minRating: 4000, maxRating: 99999, icon: 'grandmaster', color: '#FF6B6B' },
  ];

  for (const rank of ranks) {
    await prisma.rank.upsert({
      where: { id: rank.id },
      update: rank,
      create: rank,
    });
  }
  console.log('Ranks created');

  // Create season
  const now = new Date();
  const seasonEnd = new Date(now.getTime() + 60 * 24 * 60 * 60 * 1000);

  await prisma.season.upsert({
    where: { id: 1 },
    update: {},
    create: {
      id: 1,
      name: 'Season 1: Launch',
      startDate: now,
      endDate: seasonEnd,
      isActive: true,
    },
  });
  console.log('Season created');

  // Create game modes
  const gameModes = [
    {
      modeId: 'battle_royale',
      name: 'Battle Royale',
      description: 'Last player standing wins. Fight to be the champion!',
      minPlayers: 2,
      maxPlayers: 16,
      isTeamBased: false,
    },
    {
      modeId: 'deathmatch',
      name: 'Deathmatch',
      description: 'Free-for-all. Most kills wins!',
      minPlayers: 2,
      maxPlayers: 8,
      isTeamBased: false,
    },
    {
      modeId: 'team_deathmatch',
      name: 'Team Deathmatch',
      description: 'Team vs Team. Coordinate with your team to win!',
      minPlayers: 4,
      maxPlayers: 8,
      isTeamBased: true,
    },
    {
      modeId: 'capture_flag',
      name: 'Capture the Flag',
      description: 'Capture the enemy flag and bring it to your base!',
      minPlayers: 4,
      maxPlayers: 8,
      isTeamBased: true,
    },
    {
      modeId: 'race',
      name: 'Race',
      description: 'Race to the finish line. Speed is everything!',
      minPlayers: 1,
      maxPlayers: 8,
      isTeamBased: false,
    },
  ];

  for (const mode of gameModes) {
    await prisma.gameMode.upsert({
      where: { modeId: mode.modeId },
      update: mode,
      create: mode,
    });
  }
  console.log('Game modes created');

  // Create shop items
  const shopItems = [
    {
      itemId: 'skin_warrior',
      name: 'Warrior Skin',
      description: 'A fierce warrior skin for your character.',
      type: 'skin',
      priceCoins: 500,
      priceGems: 50,
      rarity: 'rare',
    },
    {
      itemId: 'skin_ninja',
      name: 'Ninja Skin',
      description: 'A stealthy ninja skin with special effects.',
      type: 'skin',
      priceCoins: 1000,
      priceGems: 100,
      rarity: 'epic',
    },
    {
      itemId: 'skin_royal',
      name: 'Royal Skin',
      description: 'A majestic royal skin for true champions.',
      type: 'skin',
      priceCoins: 0,
      priceGems: 500,
      rarity: 'legendary',
    },
    {
      itemId: 'emote_dance',
      name: 'Dance Emote',
      description: 'Show off your moves!',
      type: 'emote',
      priceCoins: 200,
      priceGems: 20,
      rarity: 'common',
    },
    {
      itemId: 'emote_victory',
      name: 'Victory Emote',
      description: 'Celebrate your wins in style!',
      type: 'emote',
      priceCoins: 300,
      priceGems: 30,
      rarity: 'rare',
    },
    {
      itemId: 'effect_trail_fire',
      name: 'Fire Trail',
      description: 'Leave a trail of fire behind you!',
      type: 'effect',
      priceCoins: 400,
      priceGems: 40,
      rarity: 'rare',
    },
    {
      itemId: 'effect_explosion',
      name: 'Death Explosion',
      description: 'Go out with a bang!',
      type: 'effect',
      priceCoins: 500,
      priceGems: 50,
      rarity: 'epic',
    },
    {
      itemId: 'boost_xp_1h',
      name: '1 Hour XP Boost',
      description: 'Double XP for 1 hour!',
      type: 'boost',
      priceCoins: 100,
      priceGems: 10,
      rarity: 'common',
    },
    {
      itemId: 'boost_coins_1h',
      name: '1 Hour Coin Boost',
      description: 'Double coins for 1 hour!',
      type: 'boost',
      priceCoins: 100,
      priceGems: 10,
      rarity: 'common',
    },
    {
      itemId: 'avatar_frame_gold',
      name: 'Gold Avatar Frame',
      description: 'A shiny gold frame for your avatar!',
      type: 'avatar_frame',
      priceCoins: 250,
      priceGems: 25,
      rarity: 'rare',
    },
  ];

  for (const item of shopItems) {
    await prisma.shopItem.upsert({
      where: { itemId: item.itemId },
      update: item,
      create: item,
    });
  }
  console.log('Shop items created');

  // Create demo users
  const demoUsers = [
    {
      email: 'demo@socialarcade.com',
      username: 'DemoPlayer',
      password: 'demo123',
    },
    {
      email: 'bot1@socialarcade.com',
      username: 'BotPlayer1',
      password: 'bot123',
    },
    {
      email: 'bot2@socialarcade.com',
      username: 'BotPlayer2',
      password: 'bot123',
    },
  ];

  for (const user of demoUsers) {
    const existingUser = await prisma.user.findUnique({
      where: { email: user.email },
    });

    if (!existingUser) {
      const passwordHash = await bcrypt.hash(user.password, 12);
      
      await prisma.user.create({
        data: {
          email: user.email,
          username: user.username,
          passwordHash,
          playerProfile: {
            create: {
              displayName: user.username,
              level: Math.floor(Math.random() * 50) + 1,
              xp: Math.floor(Math.random() * 10000),
              wins: Math.floor(Math.random() * 100),
              losses: Math.floor(Math.random() * 50),
              gamesPlayed: Math.floor(Math.random() * 200),
            },
          },
          currencies: {
            create: {
              coins: Math.floor(Math.random() * 10000) + 1000,
              gems: Math.floor(Math.random() * 500),
            },
          },
        },
      });
    }
  }
  console.log('Demo users created');

  console.log('Database seeded successfully!');
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
