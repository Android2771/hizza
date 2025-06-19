docker run --name mongodb -p 27017:27017 -d mongodb/mongodb-community-server:latest

# In mongosh
use HizzaCoin
db.createCollection("Accounts")
db.Accounts.insertMany([
  { DiscordId: "252190585597853707", Amount: 1131, Date: ISODate("2025-06-11T00:00:00Z"), Streak: 0 },
  { DiscordId: "148455623313326080", Amount: 287, Date: ISODate("2025-06-19T00:00:00Z"), Streak: 5 },
  { DiscordId: "183256785455415296", Amount: 28, Date: ISODate("2023-09-10T00:00:00Z"), Streak: 0 },
  { DiscordId: "705818395576369242", Amount: 12, Date: ISODate("2022-08-26T00:00:00Z"), Streak: 0 },
  { DiscordId: "106128795567685632", Amount: 63, Date: ISODate("2022-09-07T00:00:00Z"), Streak: 0 },
  { DiscordId: "541025799629832193", Amount: 1558, Date: ISODate("2025-05-29T00:00:00Z"), Streak: 0 },
  { DiscordId: "579908248782635008", Amount: 2, Date: ISODate("2025-02-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "214008991880052746", Amount: 105, Date: ISODate("2022-09-13T00:00:00Z"), Streak: 0 },
  { DiscordId: "342294736784064513", Amount: 0, Date: ISODate("2022-07-28T00:00:00Z"), Streak: 0 },
  { DiscordId: "693506604020007002", Amount: 17, Date: ISODate("2022-08-03T00:00:00Z"), Streak: 0 },
  { DiscordId: "141561056282542080", Amount: 25, Date: ISODate("2022-08-23T00:00:00Z"), Streak: 0 },
  { DiscordId: "188270323374882816", Amount: 10, Date: ISODate("2022-08-08T00:00:00Z"), Streak: 0 },
  { DiscordId: "1001733950693179402", Amount: 0, Date: ISODate("2022-08-14T00:00:00Z"), Streak: 0 },
  { DiscordId: "225676494007959562", Amount: 33, Date: ISODate("2025-06-17T00:00:00Z"), Streak: 0 },
  { DiscordId: "300670808236228609", Amount: 56, Date: ISODate("2022-09-16T00:00:00Z"), Streak: 0 },
  { DiscordId: "236478857849339905", Amount: 290, Date: ISODate("2025-06-19T00:00:00Z"), Streak: 5 },
  { DiscordId: "434393328415408128", Amount: 16, Date: ISODate("2022-09-15T00:00:00Z"), Streak: 0 },
  { DiscordId: "400456877131038730", Amount: 20, Date: ISODate("2022-09-20T00:00:00Z"), Streak: 0 },
  { DiscordId: "295468498526797824", Amount: 4, Date: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "242356371087949824", Amount: 57, Date: ISODate("2022-08-26T00:00:00Z"), Streak: 0 },
  { DiscordId: "344546877959700491", Amount: 5, Date: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "321213415265796098", Amount: 466, Date: ISODate("2023-09-10T00:00:00Z"), Streak: 0 },
  { DiscordId: "183577847418322944", Amount: 33, Date: ISODate("2025-06-19T00:00:00Z"), Streak: 2 },
  { DiscordId: "702539589545623704", Amount: 5, Date: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "401428500717895700", Amount: 5, Date: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "220179010712240128", Amount: 4, Date: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "453174004765818890", Amount: 8, Date: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "755194521679298630", Amount: 14, Date: ISODate("2022-11-30T00:00:00Z"), Streak: 0 },
  { DiscordId: "111898381047140352", Amount: 10, Date: ISODate("2022-11-30T00:00:00Z"), Streak: 0 },
  { DiscordId: "681096336392585255", Amount: 164, Date: ISODate("2024-02-28T00:00:00Z"), Streak: 0 },
  { DiscordId: "585468229703565333", Amount: 5, Date: ISODate("2001-01-01T00:00:00Z"), Streak: 0 },
  { DiscordId: "663424851691831341", Amount: 279, Date: ISODate("2024-06-10T00:00:00Z"), Streak: 0 },
  { DiscordId: "231082998974513153", Amount: 13, Date: ISODate("2025-05-04T00:00:00Z"), Streak: 0 },
  { DiscordId: "446656981831385088", Amount: 5, Date: ISODate("2024-12-05T00:00:00Z"), Streak: 0 },
  { DiscordId: "586278312809201665", Amount: 3392, Date: ISODate("2025-06-19T00:00:00Z"), Streak: 5 },
  { DiscordId: "256473982444765186", Amount: 227, Date: ISODate("2025-06-16T00:00:00Z"), Streak: 1 },
  { DiscordId: "513241672964898817", Amount: 14, Date: ISODate("2025-06-17T00:00:00Z"), Streak: 0 }
]);
