docker run --name mongodb -p 27017:27017 -v ~/RiderProjects/hizzacoin/HizzaCoinBackend/resources/mongo-data:/data/db -d mongodb/mongodb-community-server:latest

# In mongosh
use HizzaCoin
db.createCollection("Accounts")
db.createCollection("Transactions")
db.createCollection("Challenges")
db.createCollection("Rewards")

db.Accounts.insertMany([
  { DiscordId: "252190585597853707", Balance: 1146, ReservedBalance: 0, LastClaimDate: ISODate("2025-06-29T00:00:00Z"), Streak: 0 },
  { DiscordId: "148455623313326080", Balance: 594, ReservedBalance: 0, LastClaimDate: ISODate("2025-07-10T00:00:00Z"), Streak: 10 },
  { DiscordId: "183256785455415296", Balance: 28, ReservedBalance: 0, LastClaimDate: ISODate("2023-09-10T00:00:00Z"), Streak: 0 },
  { DiscordId: "705818395576369242", Balance: 12, ReservedBalance: 0, LastClaimDate: ISODate("2022-08-26T00:00:00Z"), Streak: 0 },
  { DiscordId: "106128795567685632", Balance: 63, ReservedBalance: 0, LastClaimDate: ISODate("2022-09-07T00:00:00Z"), Streak: 0 },
  { DiscordId: "541025799629832193", Balance: 1556, ReservedBalance: 0, LastClaimDate: ISODate("2025-05-29T00:00:00Z"), Streak: 0 },
  { DiscordId: "579908248782635008", Balance: 2, ReservedBalance: 0, LastClaimDate: ISODate("2025-02-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "214008991880052746", Balance: 105, ReservedBalance: 0, LastClaimDate: ISODate("2022-09-13T00:00:00Z"), Streak: 0 },
  { DiscordId: "342294736784064513", Balance: 0, ReservedBalance: 0, LastClaimDate: ISODate("2022-07-28T00:00:00Z"), Streak: 0 },
  { DiscordId: "693506604020007002", Balance: 17, ReservedBalance: 0, LastClaimDate: ISODate("2022-08-03T00:00:00Z"), Streak: 0 },
  { DiscordId: "141561056282542080", Balance: 25, ReservedBalance: 0, LastClaimDate: ISODate("2022-08-23T00:00:00Z"), Streak: 0 },
  { DiscordId: "188270323374882816", Balance: 10, ReservedBalance: 0, LastClaimDate: ISODate("2022-08-08T00:00:00Z"), Streak: 0 },
  { DiscordId: "1001733950693179402", Balance: 0, ReservedBalance: 0, LastClaimDate: ISODate("2022-08-14T00:00:00Z"), Streak: 0 },
  { DiscordId: "225676494007959562", Balance: 33, ReservedBalance: 0, LastClaimDate: ISODate("2025-06-17T00:00:00Z"), Streak: 0 },
  { DiscordId: "300670808236228609", Balance: 56, ReservedBalance: 0, LastClaimDate: ISODate("2022-09-16T00:00:00Z"), Streak: 0 },
  { DiscordId: "236478857849339905", Balance: 2518, ReservedBalance: 0, LastClaimDate: ISODate("2025-07-10T00:00:00Z"), Streak: 10 },
  { DiscordId: "434393328415408128", Balance: 16, ReservedBalance: 0, LastClaimDate: ISODate("2022-09-15T00:00:00Z"), Streak: 0 },
  { DiscordId: "400456877131038730", Balance: 20, ReservedBalance: 0, LastClaimDate: ISODate("2022-09-20T00:00:00Z"), Streak: 0 },
  { DiscordId: "295468498526797824", Balance: 4, ReservedBalance: 0, LastClaimDate: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "242356371087949824", Balance: -1943, ReservedBalance: 0, LastClaimDate: ISODate("2022-08-26T00:00:00Z"), Streak: 0 },
  { DiscordId: "344546877959700491", Balance: 5, ReservedBalance: 0, LastClaimDate: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "321213415265796098", Balance: 466, ReservedBalance: 0, LastClaimDate: ISODate("2023-09-10T00:00:00Z"), Streak: 0 },
  { DiscordId: "183577847418322944", Balance: -1852, ReservedBalance: 0, LastClaimDate: ISODate("2025-07-10T00:00:00Z"), Streak: 0 },
  { DiscordId: "702539589545623704", Balance: 5, ReservedBalance: 0, LastClaimDate: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "401428500717895700", Balance: 5, ReservedBalance: 0, LastClaimDate: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "220179010712240128", Balance: 4, ReservedBalance: 0, LastClaimDate: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "453174004765818890", Balance: 8, ReservedBalance: 0, LastClaimDate: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "755194521679298630", Balance: 14, ReservedBalance: 0, LastClaimDate: ISODate("2022-11-30T00:00:00Z"), Streak: 0 },
  { DiscordId: "111898381047140352", Balance: 10, ReservedBalance: 0, LastClaimDate: ISODate("2022-11-30T00:00:00Z"), Streak: 0 },
  { DiscordId: "681096336392585255", Balance: 164, ReservedBalance: 0, LastClaimDate: ISODate("2024-02-28T00:00:00Z"), Streak: 0 },
  { DiscordId: "585468229703565333", Balance: 5, ReservedBalance: 0, LastClaimDate: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "663424851691831341", Balance: 279, ReservedBalance: 0, LastClaimDate: ISODate("2024-06-10T00:00:00Z"), Streak: 0 },
  { DiscordId: "231082998974513153", Balance: 13, ReservedBalance: 0, LastClaimDate: ISODate("2025-05-04T00:00:00Z"), Streak: 0 },
  { DiscordId: "446656981831385088", Balance: 5, ReservedBalance: 0, LastClaimDate: ISODate("2024-12-05T00:00:00Z"), Streak: 0 },
  { DiscordId: "586278312809201665", Balance: 4746, ReservedBalance: 0, LastClaimDate: ISODate("2025-07-10T00:00:00Z"), Streak: 10 },
  { DiscordId: "256473982444765186", Balance: 261, ReservedBalance: 0, LastClaimDate: ISODate("2025-07-09T00:00:00Z"), Streak: 1 },
  { DiscordId: "513241672964898817", Balance: 14, ReservedBalance: 0, LastClaimDate: ISODate("2025-06-17T00:00:00Z"), Streak: 0 }
])


db.Rewards.insertMany([
  { Streak: 10, RewardedAmount: 40 },
  { Streak: 20, RewardedAmount: 80 },
  { Streak: 30, RewardedAmount: 120 },
  { Streak: 60, RewardedAmount: 400 },
  { Streak: 90, RewardedAmount: 500 },
  { Streak: 150, RewardedAmount: 1200 },
  { Streak: 200, RewardedAmount: 1500 },
  { Streak: 365, RewardedAmount: 2000 },
  { Streak: 548, RewardedAmount: 3500 },
  { Streak: 720, RewardedAmount: 4000 }
])