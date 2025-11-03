
//SYNC COMMANDS
import { REST, Routes, Client, GatewayIntentBits, Partials, ButtonBuilder, ButtonStyle, ActionRowBuilder, ButtonInteraction, ChatInputCommandInteraction, Interaction, EmbedBuilder } from 'discord.js';
import process from 'process';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import fetch from "node-fetch";
import { Chess } from 'chess.js'
import { execSync } from 'child_process';
import 'dotenv/config';
import OpenAI from "openai";
import puppeteer  from 'puppeteer';

interface Account {
  Id: string | null;
  DiscordId: string;
  Balance: number;
  ReservedBalance: number;
  LastClaimDate: string;
  Streak: number;
}

interface Reward {
  Id: string | null;
  Streak: number;
  RewardedAmount: number;
}

interface CoinClaimResponse {
  DiscordId: string;
  BaseClaim: number;
  Streak: number;
  Multiplier: number;
  ClaimedReward: Reward;
  NextReward: Reward;
  TotalClaim: number;
  Claimed: boolean;
}

interface CoinBalanceResponse {
  Balance: number;
  WageredBalance: number;
}

interface CoinEconomyResponse {
  TotalHizzaCoinAmount: number;
  TotalHizzaCoinAccounts: number;
  LeaderboardPlace: number;
  PercentageEconomy: number;
}

interface RouletteResponse {
  RouletteNumber : number;
  Bet : number;
  Payout : number;
}

export enum ChallengeState {
  InProgress = 0,
  PlayerOneWin = 1,
  PlayerTwoWin = 2,
  Draw = 3,
  Expired = 4
}

export enum Hand {
  NotSelected = 0,
  Rock = 1,
  Paper = 2,
  Scissors = 3
}

export interface Challenge {
  Id?: string;
  ChallengerDiscordId: string;
  ChallengedDiscordId: string;
  Wager: number;
  Date: string; // ISO 8601 date string
  ChallengerHand: Hand;
  ChallengedHand: Hand;
  State: ChallengeState;
}


const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const token = process.env["DISCORD_BOT_TOKEN"];
const openai = new OpenAI({
  apiKey: process.env["OPEN_AI_KEY"],
});
const botId = atob(token!.split('.')[0]);
const usernameCache: { [key: string]: string } = {}

const client = new Client(
  {
    intents: [GatewayIntentBits.DirectMessages,
    GatewayIntentBits.Guilds,
    GatewayIntentBits.GuildMessages,
    GatewayIntentBits.MessageContent,
    GatewayIntentBits.GuildMembers,
    GatewayIntentBits.DirectMessageReactions,
    GatewayIntentBits.GuildMessageReactions,
    GatewayIntentBits.GuildPresences
    ], partials: [Partials.Channel]
  })

let chess : any;
let commandsExecuted = 0;

let chessOngoing = false;
let player1 = '';
let player2 = '';
let whitePlaying : boolean;

const gptGuilds = ["1249401431262232636", "1167198223832723476", "954741402586185778", "841363743957975063", "1278753068669993012"]
let temperature  = 1.0;
let max_tokens = 512;
let chatModel = 'gpt-4-turbo';
let imageModel = 'dall-e-2';
let behaviour = "";
let top_p = 1;

if (process.argv[2]) {
  const commands = [
    {
      name: 'coinclaim',
      description: 'Claim your daily HizzaCoin',
    },
    {
      name: 'coinbalance',
      description: 'See your HizzaCoin balance',
      options: [
        {
          name: 'person',
          description: 'Whose wallet to look at',
          required: false,
          type: 6
        }
      ]
    },
    {
      name: 'coinleaderboard',
      description: 'See the HizzaCoin leaderboard'
    },
    {
      name: 'coineconomy',
      description: 'See the HizzaCoin economy'
    },
    {
      name: 'coindelete',
      description: 'Delete your HizzaCoin account for GDPR reasons'
    },
    {
      name: 'coingive',
      description: 'Give HizzaCoin to a friend',
      options: [
        {
          name: 'payee',
          description: 'The user to give HizzaCoin to',
          required: true,
          type: 6
        },
        {
          name: 'amount',
          description: 'The amount of HizzaCoin to give',
          required: true,
          type: 4,
          min_value: 1
        }
      ]
    },
    {
      name: 'challenge',
      description: 'Challenge your foes to rock paper scissors, perhaps with a wager',
      options: [
        {
          name: 'opponent',
          description: 'Who shall you challenge to rock paper scissors',
          required: true,
          type: 6
        },
        {
          name: 'wager',
          description: 'Optionally wager hizzacoin in your challenge',
          required: false,
          type: 4,
          min_value: 1
        }
      ]
    },
    {
      name: 'roll',
      description: 'Roll a dice',
      options: [
        {
          name: 'maximum',
          description: 'How many sides to the dice (default is 6)',
          required: false,
          type: 4,
          min_value: 1
        }
      ]
    },
    {
      name: 'flipcoin',
      description: 'Flip a coin'
    },
    {
      name: 'dadjoke',
      description: "Give me a dad joke that'll give me the giggles"
    },
    {
      name: 'destiny',
      description: "I want to know today's destiny."
    },
    {
      name: "tell",
      description: "Hizza is now sentient, talk to her",
      options: [
        {
          name: "prompt",
          description: "What to tell hizza",
          required: true,
          type: 3
        }
      ]
    },
    {
      name: "imagine",
      description: "Hizza generates images now",
      options: [
        {
          name: "prompt",
          description: "What image to generate",
          required: true,
          type: 3
        }
      ]
    },
    {
      name: "guessnumbers",
      description: "Guess numbers between 0 and 36 with a chance to win up to x36 your HizzaCoin",
      options: [
        {
          name: "numbers",
          description: "Which numbers between 0 and 36",
          required: true,
          type: 3
        },
        {
          name: 'wager',
          description: 'How much HizzaCoin to wager',
          required: true,
          type: 4,
          min_value: 1
        }
      ]
    },
    {
      name: "guesscolour",
      description: "Guess what colour the number is with a chance to win x2 HizzaCoin",
      options: [
        {
          name: "red",
          description: "True if you guess number is red, False if number is black",
          required: true,
          type: 5
        },
        {
          name: 'wager',
          description: 'How much hizzacoin',
          required: true,
          type: 4,
          min_value: 1
        }
      ]
    },
    {
      name: "guesstwelve",
      description: "Guess what multiple of 12 number will belong to up to 3, with a chance to win x3 HizzaCoin",
      options: [
        {
          name: "twelve",
          description: "Which multiple of 12, 1, 2 or 3",
          required: true,
          type: 4,                 
          min_value: 1,
          max_value: 3
        },
        {
          name: 'wager',
          description: 'How much hizzacoin',
          required: true,
          type: 4,
          min_value: 1
        }
      ]
    }
  ];

  const rest = new REST({ version: '10' }).setToken(token!);
  (async () => {
    try {
      console.log('Started refreshing application (/) commands.');

      await rest.put(Routes.applicationCommands(botId), { body: commands });

      console.log('Successfully reloaded application (/) commands.');
    } catch (error) {
      console.error(error);
    }
  })();
}

client.on('ready', () => {
  console.log(`Logged in as ${client.user?.tag}!`);
});

client.on('presenceUpdate', async (oldPresence, newPresence) => {
if(newPresence!.guild!.id !== "841363743957975063")
  return;

 let status : number;
 switch(newPresence.status){
   case "online": status = 0; break;
   case "idle": status = 1; break;
   case "dnd": status = 2; break;
   case "offline": status = 3; break;
   default: status = -1; break;
 }
})

client.on("messageCreate", async (message : any) => {
  // If bot is saying it then ignore, bot should not execute commands
  OUTER_LOOP: {
    if (message.author.id === botId || message.author.id === '255240051573653505' || message.author.id === "341911947366891531")
      break OUTER_LOOP

    if (message.channel.name === "chess") {
      if (!chessOngoing) {
        if (message.content.startsWith("challenge ") && player1 === '' && player2 === '' && message.content.split(' ').length === 2 && message.content.endsWith('>')) {
          player1 = message.author.id;
          let start = message.content[2] === '!' ? 3 : 2
          player2 = message.content.substr("challenge ".length + start, "183577847418322944".length)
          setTimeout(() => {
            if (!chessOngoing) {
              player1 = ''
              player2 = ''
              message.react('⏰')
            }
          }, 3600000);
          if (player1 !== player2)
            message.react("👌")
          else {
            player1 = ''
            player2 = ''
          }
        } else if (message.content === "accept" && message.author.id === player2) {
          chessOngoing = true;
          whitePlaying = true;
          chess = new Chess();
          message.channel.send(`https://chessboardimage.com/${encodeURI(chess.fen())}.png`);
          message.react("👌")
        } else if (message.content === "reject" && message.author.id === player2) {
          player1 = ''
          player2 = ''
        }
      } else {
        if (message.content === "forfeit") {
          chess = new Chess();
          player1 = ''
          player2 = ''
          chessOngoing = false;
          message.react("🚩")
          break OUTER_LOOP
        } else if ((whitePlaying && message.author.id === player1) || (!whitePlaying && message.author.id === player2)) {
          if (chess.move(message.content)) {
            message.channel.send(whitePlaying ? `https://chessboardimage.com/${encodeURI(chess.fen())}-flip.png` : `https://chessboardimage.com/${encodeURI(chess.fen())}.png`);
            if (chess.game_over()) {
              if (chess.in_checkmate())
                if (whitePlaying)
                  message.react("🤍")
                else
                  message.react("🖤")
              else if (chess.in_draw() || chess.in_stalemate() || chess.in_threefold_repetition()) {
                message.react("🤍")
                message.react("🖤")
              }
              chess = new Chess();
              player1 = ''
              player2 = ''
              chessOngoing = false;
              break OUTER_LOOP
            }
            if (chess.in_check())
              message.react("🚩")
            whitePlaying = !whitePlaying;
            message.react("👌")
          } else {
            message.react("❌")
          }
        }
      }
      break OUTER_LOOP
    }

    if (!message.guildId) {
      let msg = `*${message.author.username}*: ${message.content}`
      sendMessage('183577847418322944', msg)

      //If not a developer, do not execute commands on dm's
      let devs = ['183577847418322944', '252190585597853707']
      if (!devs.includes(message.author.id))
        break OUTER_LOOP
    }

    if (message.content === "howlong") {
      if (message.author.id === "225676494007959562" || message.author.id === "236478857849339905")
          message.channel.send(`Drea and Cath have been dating for \`${((Date.now() - (new Date(2020, 1, 21).valueOf())) / 1000 / 60 / 60 / 24 / 365.25).toFixed(5)}\` years!`)
      else if (message.author.id === "183577847418322944" || message.author.id === "148455623313326080")
          message.channel.send(`Andy Pandy and Nasi Poo have been dating for \`${((Date.now() - (new Date(2021, 7, 7)).valueOf()) / 1000 / 60 / 60 / 24 / 365.25).toFixed(5)}\` years!`)
      else if (message.author.id === "586278312809201665" || message.author.id === "196171123019218944")
	  message.channel.send(`Anne and Chad have been dating for \`${((Date.now() - (new Date(2022, 11, 27)).valueOf()) / 1000 / 60 / 60 / 24 / 365.25).toFixed(5)}\` years!`)
      break OUTER_LOOP
    }

    //EASTER EGGS
    //"funny" implies "the big laugh"
    if (message.content === "FUNNY") {
        message.channel.send("THE BIG LAUGH");
    }

    //HIZZA LIKES TAYLOR SWIFT
    if (message.content === "tay") {
        message.channel.send("I LOVE TAYLOR SWIFT 🤩😛🤯😍😱");
    }

    if (message.content.toLowerCase().includes("big destiny")) {
        message.react("🐳")
    }

    if (message.content.toLowerCase().includes("can we get 5 likes")) {
        message.react("👍🏻")
        message.react("👍🏼")
        message.react("👍🏽")
        message.react("👍🏾")
        message.react("👍🏿")
    }

    if (message.content.toLowerCase().includes("can we get 5 dislikes")) {
        message.react("👎🏻")
        message.react("👎🏼")
        message.react("👎🏽")
        message.react("👎🏾")
        message.react("👎🏿")
    }

    if (message.content.toLowerCase().includes(" bean ") || message.content.toLowerCase().startsWith("bean ") || message.content.toLowerCase().endsWith(" bean")) {
        message.react("🇧")
        message.react("🇪")
        message.react("🇦")
        message.react("🇳")
    }


    //Owner only commands
    if (message.author.id === "183577847418322944") {      
      if (message.content.startsWith("exec ")) {
        let output
        try {
          output = `\`\`\`\n${execSync(message.content.substr(5), { encoding: 'utf-8' })}\`\`\``
          message.channel.send(output)
        } catch (err) {
          message.react("❌");
        } finally {
          break OUTER_LOOP
        }
      }

      if (message.content.startsWith("eval ")) {
        let output
        try {
          output = `\`\`\`\n${eval(message.content.substr(5)).toString()}\`\`\``
          message.channel.send(output)
        } catch (err) {
          message.react("❌");
        } finally {
          break OUTER_LOOP
        }
      }

      if(message.content.startsWith("visit ")){
        puppeteer.launch({
            args: [
              '--no-sandbox',
              '--disable-setuid-sandbox',
              '--disable-dev-shm-usage',
              '--disable-session-crashed-bubble',
              '--disable-accelerated-2d-canvas',
              '--disable-gpu'
            ],
            headless: true,
            executablePath: '/usr/bin/chromium-browser',
            defaultViewport: {
            width: 1080,
            height: 720,            
            },
        }).then(async (browser) => {
            try{
                message.react("🔎")
                const page = await browser.newPage();
                await page.goto(message.content.substr(6));
                await page.screenshot({ path: "page.png" });

                message.channel.send({files: ['page.png']});
                await browser.close();
            }catch(err){
                message.react("❌")
            }
        });
        }
    }   
  }
})

client.on('interactionCreate', async (interaction: Interaction) => {
  if (!interaction.isChatInputCommand()) return;

  commandsExecuted++;
  
  let oldLeaderboard : Account[] = [];
  if(interaction.commandName.startsWith("coin") || interaction.commandName.startsWith("guess") || interaction.commandName === "challenge"){
    oldLeaderboard = await (await fetch(`http://localhost:8080/api/coin-commands/coin-leaderboard`)).json();
  }

  switch (interaction.commandName) {
    case "coinclaim":         try { await coinClaim(interaction); }       catch (err) { console.error(err) } break;
    case "coinbalance":       try { await coinBalance(interaction); }     catch (err) { console.error(err) } break;
    case "coinleaderboard":   try { await coinLeaderboard(interaction); } catch (err) { console.error(err) } break;
    case "coineconomy":       try { await coinEconomy(interaction); }     catch (err) { console.error(err) } break;
    case "coindelete":        try { await coinDelete(interaction); }      catch (err) { console.error(err) } break;
    case "coingive":          try { await coinGive(interaction); }        catch (err) { console.error(err) } break;
    case "challenge":         try { await challenge(interaction); }       catch (err) { console.error(err) } break;
    case "roll":              try { await roll(interaction); }            catch (err) { console.error(err) } break;
    case "flipcoin":          try { await flipcoin(interaction); }        catch (err) { console.error(err) } break;
    case "dadjoke":           try { await dadJoke(interaction); }         catch (err) { console.error(err) } break;
    case "destiny":           try { await destiny(interaction); }         catch (err) { console.error(err) } break;
    case "tell":              try { await tell(interaction); }            catch (err) { console.error(err) } break;
    case "imagine":           try { await imagine(interaction); }         catch (err) { console.error(err) } break;
    case "guessnumbers":    try { await rouletteNumber(interaction); }  catch (err) { console.error(err) } break;
    case "guesscolour":    try { await rouletteColour(interaction); }  catch (err) { console.error(err) } break;
    case "guesstwelve":   try { await rouletteTwelves(interaction); } catch (err) { console.error(err) } break;
  }
  if(oldLeaderboard){    
    let newLeaderboard : Account[] = await (await fetch(`http://localhost:8080/api/coin-commands/coin-leaderboard`)).json();
    oldLeaderboard.filter(x => !newLeaderboard.includes(x)).forEach(removedFromLeaderboard => {
      updateMedal(removedFromLeaderboard.DiscordId, 4)
    });

    for(let i = 0; i < newLeaderboard.length && i < oldLeaderboard.length; i++){  

      //Update medal for leaderboard place changes or if first command
      if(oldLeaderboard[i].DiscordId !== newLeaderboard[i].DiscordId || commandsExecuted === 1){
        updateMedal(newLeaderboard[i].DiscordId, i+1);
      }
    };
  }
});

export async function coinClaim(interaction: ChatInputCommandInteraction) {
  if(interaction){  
    const response : CoinClaimResponse = await (await fetch(`http://localhost:8080/api/coin-commands/coin-claim?discordId=${interaction.user.id}`)).json();
    
    let responseText = `<@${interaction.user.id}> CLAIMED \`${response["BaseClaim"]}\` COIN!\n`

    if(response.BaseClaim === 0)
      responseText = "You have already claimed your coin!";
    else{
      if(response.Streak > 0)
        responseText += `\`+${Math.min(response.Streak, 30)}\` Streak ${response.Streak >= 10 ? 'MAX' : ''}\n`;
      if(response.ClaimedReward.RewardedAmount > 0)
        responseText += `\`+${response.ClaimedReward.RewardedAmount}\` Reward for \`${response.ClaimedReward.Streak}\` Streak\n`;
      if(response.Multiplier > 1)
        responseText += `\`x${response.Multiplier}\` MULTIPLIER! 🪙🪙\n`;
      
      responseText += `\n**TOTAL COIN CLAIMED:** \`${response.TotalClaim}\` 🪙\n`
      if(response.NextReward)
        responseText += `Next Reward is in \`${response.NextReward.Streak - response.Streak}\` days!   (Streak Progress: \`${response.Streak}\`/\`${response.NextReward.Streak}\`)\n`;
    }

    await interaction.reply(responseText);
  }
}

export async function coinBalance(interaction: ChatInputCommandInteraction) {
  if(interaction){
    const response : CoinBalanceResponse = await (await fetch(`http://localhost:8080/api/coin-commands/coin-balance?discordId=${interaction.options!.get('person') ? interaction.options!.get('person')!.user!.id! : interaction.user.id}`)).json();
    let responseText = "";
    if(interaction.options!.get('person'))
      responseText += `<@${interaction.options!.get('person')!.user!.id!}> has \`${response.Balance}\` HizzaCoin 🪙`;
    else
      responseText += `You have \`${response.Balance}\` HizzaCoin 🪙`;

    if(response.WageredBalance > 0)
      responseText += `  (\`${response.WageredBalance}\` of which is reserved)`

    await interaction.reply(responseText);
  }
}

export async function coinLeaderboard(interaction: ChatInputCommandInteraction) {
  if(interaction){  
    const response : Account[] = await (await fetch(`http://localhost:8080/api/coin-commands/coin-leaderboard`)).json();
    await interaction.deferReply();
    
    let leaderboardText = "**...................  LeaderBoard  ....................**\n";
    for(let i = 0; i < response.length; i++){
      let username = await fetchUsername(response[i].DiscordId);
      leaderboardText += `${i < 3 ? ["🥇", "🥈", "🥉"][i] : i+1+")"} **${username.padEnd(15, " ")}** with **${response[i].Balance.toString().padStart(5, " ")}** HizzaCoin\n`;
    };
    
    await interaction.editReply(leaderboardText);
  }
}

async function updateMedal(discordId : string, place : number){
  try{
    const guild = await client.guilds.fetch("841363743957975063");
    const member = await guild.members.fetch(discordId);
    const nickname = member.nickname;
    if(nickname !== null)
      await member.setNickname(place < 4 ? `${nickname.split(" ")[0]} ${["🥇", "🥈", "🥉"][place - 1]}` : nickname.split(" ")[0])
    console.log(`Updated medal for ${discordId} with place ${place}`)
  }catch(error){
    console.log(`Could not update medal for ${discordId} with place ${place}`)
    console.log(error);
    return;
  }
}

export async function coinEconomy(interaction: ChatInputCommandInteraction) {
  if(interaction){  
    const response : CoinEconomyResponse = await(await fetch(`http://localhost:8080/api/coin-commands/coin-economy?discordId=${interaction.user.id}`)).json();
    let responseText = ""; 
    if(response === null){
      responseText = "Use 'coin claim' to get some HizzaCoin and unlock more info"
    }else{
      responseText += `🍕 HizzaCoin in Circulation: \`${response.TotalHizzaCoinAmount}\`\n`
      responseText += `🔢 Total HizzaCoin Accounts: \`${response.TotalHizzaCoinAccounts}\`\n`
      responseText += `📊 Your place on the LeaderBoard: \`${response.LeaderboardPlace}\`\n`
      responseText += `🥧 You own: \`${response.PercentageEconomy}\`% of the economy\n`
    }

    await interaction.reply(responseText)
  }
}

export async function coinDelete(interaction: ChatInputCommandInteraction) {
  if(interaction){
    await interaction.reply("🔔 To delete your account contact ||info@andrewbuhagiar.com|| 🔔")
  }
}

export async function coinGive(interaction: ChatInputCommandInteraction | undefined) {
  if(interaction){
    const senderDiscordId = interaction.user.id;
    const receiverDiscordId = interaction.options!.get('payee')!.user!.id;
    const amountToSend = parseInt((interaction.options!.get('amount')!.value)!.toString());

    if(senderDiscordId === receiverDiscordId)
      return await interaction.reply({content: "You cannot send HizzaCoin to yourself!", ephemeral: true})
    if(amountToSend <= 0)
      return await interaction.reply({content: "You must send at least 1 HizzaCoin!", ephemeral: true})
    const response = await (await fetch(`http://localhost:8080/api/coin-commands/coin-give?senderDiscordId=${senderDiscordId}&receiverDiscordId=${receiverDiscordId}&amountToSend=${amountToSend}`)).json();
    if(response.status === 400 || !response)
      return await interaction.reply({content: "You do not have enough HizzaCoin to perform this action! Try `coin claim` to get more", ephemeral: true})
    return await interaction.reply(`Sent \`${amountToSend}\` HizzaCoin to <@${receiverDiscordId}>!`)
  }
}
export async function challenge(interaction: ChatInputCommandInteraction) {
  if(interaction){    
    //Challenge initiated logic
    const opponent = interaction.options.get('opponent');

    if(opponent!.user!.id === botId){
      await interaction.reply({
        content: `You cannot challenge me!`,
        ephemeral: true
      });

      return;
    }

    let wager : number | null = interaction.options!.get('wager') ? parseInt((interaction.options!.get('wager')!.value)!.toString()) : 0;

    let initiateResponseRaw = await fetch(`http://localhost:8080/api/coin-commands/initiate-challenge?challengerDiscordId=${interaction.user.id}&challengedDiscordId=${opponent!.user!.id}&wager=${wager}`);
    let initiateResponse : Challenge;
    if(initiateResponseRaw.status !== 200){
      await interaction.reply({content: "You cannot create this challenge. Do you both have enough HizzaCoin?", ephemeral: true})
      return;
    }else{
      initiateResponse = await initiateResponseRaw.json();
    }
  
    const emotes = ["🪨", "📰", "✂️", "🚫"]
    if(initiateResponse){
        const rock = new ButtonBuilder()
        .setCustomId(`1:${initiateResponse.Id}`)
        .setLabel('Rock')
        .setStyle(ButtonStyle.Primary)
        .setEmoji(emotes[0]);

        const paper = new ButtonBuilder()
        .setCustomId(`2:${initiateResponse.Id}`)
        .setLabel('Paper')
        .setStyle(ButtonStyle.Primary)
        .setEmoji(emotes[1]);

        const scissors = new ButtonBuilder()
        .setCustomId(`3:${initiateResponse.Id}`)
        .setLabel('Scissors')
        .setStyle(ButtonStyle.Primary)
        .setEmoji(emotes[2]);

        const decline = new ButtonBuilder()
        .setCustomId(`4:${initiateResponse.Id}`)
        .setLabel('Decline')
        .setStyle(ButtonStyle.Danger)
        .setEmoji(emotes[3]);

        const responseRow : any = new ActionRowBuilder()
        .addComponents(rock, paper, scissors, decline);

        await interaction.reply({
          content: `${wager ? '🪙🪙🪙' : ''} <@${opponent!.user!.id}> has been challenged by <@${interaction.user.id}>` + (wager ? ` with a **${wager} HizzaCoin wager** 🪙🪙🪙!` : `!`),
          components: [responseRow],
        });

        const timeout = 900000;
        const buttonCollector = interaction.channel!.createMessageComponentCollector({ time: timeout });

        buttonCollector.on('collect', async (buttonInteraction : ButtonInteraction) => {
          if(buttonInteraction.customId.split(':')[1] !== initiateResponse.Id)
            return;

          const checkChallenge = await fetch(`http://localhost:8080/api/challenges/${initiateResponse.Id}`);
          if(checkChallenge.status !== 200){
            await buttonInteraction.reply({content: "The challenge does not exist", ephemeral: true})
            return;
          }else{
            const checkChallengeJson : Challenge = await checkChallenge.json();
            if(checkChallengeJson.State === ChallengeState.Expired){
              await buttonInteraction.reply({content: "This challenge has expired", ephemeral: true});
              return;
            }
          }

          if(parseInt(buttonInteraction.customId.split(":")[0]) === 4){
            if(buttonInteraction.user.id === interaction.user.id || buttonInteraction.user.id === opponent!.user!.id){
               const cancel : boolean = await (await (await fetch(`http://localhost:8080/api/coin-commands/cancel-challenge?challengeId=${initiateResponse.Id}`))).json();
               await buttonInteraction.reply(`<@${buttonInteraction.user.id}> has cancelled the challenge!`);
            }else{
               await buttonInteraction.reply({content: "You cannot cancel this challenge!", ephemeral: true})
            }

            return;
          }

          const challengeRequest = await fetch(`http://localhost:8080/api/coin-commands/respond-challenge?discordId=${buttonInteraction.user.id}&challengeId=${initiateResponse.Id}&hand=${buttonInteraction.customId.split(':')[0]}`);  

          let challenge : Challenge;
          if(challengeRequest.status === 200){
            challenge = await challengeRequest.json()
          }else{
            await buttonInteraction.reply({content: "You cannot respond to this challenge. Do you have enough HizzaCoin?", ephemeral: true})
            return;
          }

          let winMsg = "";
          switch(challenge.State){
            case ChallengeState.InProgress:
              let hand = Hand.NotSelected;
              if(challenge.ChallengerDiscordId === buttonInteraction.user.id)
                hand = challenge.ChallengerHand;
              else if(challenge.ChallengedDiscordId === buttonInteraction.user.id)
                hand = challenge.ChallengedHand
              await buttonInteraction.reply({
                content: `You chose your hand as  ${emotes[hand - 1]}!`,
                ephemeral: true
              })
            break;
            case ChallengeState.PlayerOneWin:
              winMsg = `<@${challenge.ChallengerDiscordId}> ${emotes[challenge.ChallengerHand - 1]} beat <@${challenge.ChallengedDiscordId}> ${emotes[challenge.ChallengedHand - 1]}!`
              if(wager){
                winMsg = winMsg + `\n And has won ${wager} HizzaCoin! 🪙`      
              }

              await buttonInteraction.reply(winMsg);
            break;
            case ChallengeState.PlayerTwoWin:
              winMsg = `<@${challenge.ChallengedDiscordId}> ${emotes[challenge.ChallengedHand - 1]} beat <@${challenge.ChallengerDiscordId}> ${emotes[challenge.ChallengerHand - 1]}!`
              if(wager){
                winMsg = winMsg + `\n And has won ${wager} HizzaCoin! 🪙`      
              }

              await buttonInteraction.reply(winMsg);
            break;
            case ChallengeState.Draw:
              await buttonInteraction.reply(`<@${challenge.ChallengerDiscordId}> ${emotes[challenge.ChallengerHand - 1]} tied with <@${challenge.ChallengedDiscordId}> ${emotes[challenge.ChallengedHand - 1]}!`)
            break;
            case ChallengeState.Expired:
              await buttonInteraction.reply("The challenge has expired!")
            break;
            default:
              await buttonInteraction.reply("Something went wrong!")              
          }          
        });
      }               
    }    
}

export async function tell(interaction: ChatInputCommandInteraction){
  if(interaction){  
    const prompt = interaction.options!.get('prompt')!.value!.toString()
    await interaction.deferReply();
    if(gptGuilds.includes(interaction.guildId!)){
      try{
        const params: OpenAI.Chat.ChatCompletionCreateParams = {
          messages: [            
            {
              role: "system",
              content: [
                {
                  "type": "text",
                  "text": "Limit your response to a maximum of three sentences and " + behaviour
                }
              ]
            },
            {
             role: 'user', content: [
              {
                "type": "text",
                "text": prompt
              }
            ]}],
          model: chatModel,
          max_tokens,
          temperature,
          top_p,
          stream: false,   
               
        };

        const chatCompletion: OpenAI.Chat.ChatCompletion = await openai.chat.completions.create(params);
        if(chatCompletion.choices[0].message.content)
          await interaction.editReply(`**<@${interaction.user.id}>: ${interaction.options!.get('prompt')!.value!}**\n\n${chatCompletion.choices[0].message.content}`);  
      }catch(error){
          await interaction.editReply("We ran into a problem...")
          console.error(error);
      }
    }else{
      await interaction.editReply("**GPT commands aren't available on this server**")
    }
  }else{
    return;
  }
}

export async function imagine(interaction: ChatInputCommandInteraction){
  if(interaction){
    const prompt = interaction.options!.get('prompt')!.value!.toString()
    await interaction.deferReply();

    if(gptGuilds.includes(interaction.guildId!)){
      try{
        const response = await openai.images.generate({
        model: imageModel,
        prompt,
        n: 1,
        size: "1024x1024",
        // quality: "standard"
        });
        
        const imageUrl = response.data![0].url;
        const embed = new EmbedBuilder().setColor(0x007AFF).setTitle(prompt).setImage(imageUrl!);
        await interaction.editReply({embeds: [embed]});
      }catch(error){
          await interaction.editReply("We ran into an issue...")
          console.error(error);
      }
    }else{
      await interaction.editReply("**GPT commands aren't available on this server**")
    }
  }else{
    return;
  }
}

export async function roll(interaction: ChatInputCommandInteraction) {
  if(interaction){
    let max : number | null = interaction.options!.get('maximum') ? parseInt((interaction.options!.get('maximum')!.value)!.toString()) : 6;
    if (max > 0)
        await interaction.reply(`Rolled a \`${(Math.floor(Math.random() * max) + 1).toString()}\` using a D${max}!`)
  }else{
    return;
  }
}

export async function flipcoin(interaction: ChatInputCommandInteraction) {
  if(interaction){
    if (Math.floor(Math.random() * 2))
        await interaction.reply("👍")
      else
        await interaction.reply("👎")
  }else{
    return;
  }
}

export async function dadJoke(interaction: ChatInputCommandInteraction) {
  if(interaction){
    fetch("https://icanhazdadjoke.com", {
      headers: {
        "Content-Type": "text/plain",
        "Accept": "text/plain"
      },
    }).then(async joke => {
      await interaction.reply(await joke.text())
    });
  }else{
    return;
  }
}

export async function destiny(interaction: ChatInputCommandInteraction) {
  if(interaction){  
    const destinyVal = getDestinyVal()
    switch (destinyVal) {
        case 1: await interaction.reply("https://cdn.discordapp.com/attachments/551037572277731339/821802508220366885/small.png"); break;
        case 2: await interaction.reply("https://cdn.discordapp.com/attachments/551037572277731339/821802511115092038/somewhat.png"); break;
        case 3: await interaction.reply("https://cdn.discordapp.com/attachments/551037572277731339/821802504194752533/big.png"); break;
        case 4: await interaction.reply("https://cdn.discordapp.com/attachments/551037572277731339/821802516382875654/very.png"); break;
        case 5: await interaction.reply("https://cdn.discordapp.com/attachments/551037572277731339/821802507097473045/insane.png"); break;
        default: await interaction.reply("https://cdn.discordapp.com/attachments/551037572277731339/821802504194752533/big.png"); break;
    }      
  }else{
    return;
  }  
}

export async function rouletteNumber(interaction: ChatInputCommandInteraction) {
  if(interaction){
      const takeNumberInputs = (numberInputs : string) : Set<number> => {
        if(!/^(\d+([-–]+\d+)?)(,(\d+([-–]+\d+)?))*$/.test(numberInputs))
            return new Set();

        let allNumberInputs = new Set<number>()

        numberInputs.split(",").forEach(number => {
            let dashSplitNumber = number.split(/[-–]+/)
            switch(dashSplitNumber.length){
                case 1:
                    if(parseInt(number) < 0 || parseInt(number) > 36)
                      return new Set();
                    allNumberInputs.add(parseInt(number));
                break;
                case 2:
                    let from = parseInt(dashSplitNumber[0]);
                    let to = parseInt(dashSplitNumber[1]);
                    if(to >= from){
                        for(let i = from; i <= to; i++){
                          if(i < 0 || i > 36)
                            return new Set();
                            allNumberInputs.add(i)
                        }
                    }else{
                        return new Set();
                    }
                break;
                default:
                    return new Set();
            }
        });

        return allNumberInputs
    }

    const processedInput = takeNumberInputs(interaction.options!.get('numbers')!.value!.toString());
    if(processedInput.size == 0){
      await interaction.reply("Incorrect format! Type in numbers or ranges of numbers like this: \`0,1,2,3-6,20\`")
      return;
    }
    const reward = parseFloat(((processedInput.size / 36) ** -1).toFixed(2));
    const wager : number = <number>interaction.options!.get('wager')!.value!;
    if(wager * reward < wager + 1){
      await interaction.reply("Your bet is too safe and would not net you any rewards! Try betting more money or guessing less numbers!")
      return;
    }
    const allNumberInputsString = [...processedInput].toString();
    const response : RouletteResponse = await (await fetch(`http://localhost:8080/api/coin-commands/roulette-number?discordId=${interaction.user.id}&numberBets=${allNumberInputsString}&bet=${interaction.options!.get('wager')!.value!}`)).json();
    if(response.Payout > 0){
      await interaction.reply(`You managed to guess the number \`${response.RouletteNumber}\`! Your \`${response.Bet}\` bet turned to \`${response.Payout}\` HizzaCoin (\`x${reward}\`) 🪙🪙🪙!`)
    }else if(response.Bet > 0){
      await interaction.reply(`You did not manage to guess the number \`${response.RouletteNumber}\` and lost \`${response.Bet}\` HizzaCoin`)
    }else{
      await interaction.reply(`You do not have enough money to bet! Try \`coin claim\` to get more`)
    }
  }
}

export async function rouletteColour(interaction: ChatInputCommandInteraction) {
  if(interaction){
    const response : RouletteResponse = await (await fetch(`http://localhost:8080/api/coin-commands/roulette-colour?discordId=${interaction.user.id}&isColourRedBet=${interaction.options!.get('red')!.value!}&bet=${interaction.options!.get('wager')!.value!}`)).json();
    if(response.Payout > 0){
      await interaction.reply(`You managed to guess the colour of the number \`${response.RouletteNumber}\`! Your \`${response.Bet}\` bet turned to \`${response.Payout}\` HizzaCoin (x2) 🪙🪙🪙`)
    }else if(response.Bet > 0){
      await interaction.reply(`You did not manage to guess the colour of the number \`${response.RouletteNumber}\` and lost \`${response.Bet}\` HizzaCoin`)
    }else{
      await interaction.reply(`You do not have enough money to bet! Try \`coin claim\` to get more`)
    }
  }
}

export async function rouletteTwelves(interaction: ChatInputCommandInteraction) {
  if(interaction){
    const response : RouletteResponse = await (await fetch(`http://localhost:8080/api/coin-commands/roulette-twelve?discordId=${interaction.user.id}&twelveBet=${interaction.options!.get('twelve')!.value!}&bet=${interaction.options!.get('wager')!.value!}`)).json();
    if(response.Payout > 0){
      await interaction.reply(`You managed to guess the twelve of the number \`${response.RouletteNumber}\`! Your \`${response.Bet}\` bet turned to \`${response.Payout}\` HizzaCoin (x3) 🪙🪙🪙!`)
    }else if(response.Bet > 0){
      await interaction.reply(`You did not manage to guess the twelve of the number \`${response.RouletteNumber}\` and lost \`${response.Bet}\` HizzaCoin`)
    }else{
      await interaction.reply(`You do not have enough money to bet! Try \`coin claim\` to get more`)
    }
  }
}

const getDestinyVal = () => {
  const date = new Date();
  const seed = date.getDate() + date.getMonth();
  if ((seed % 3 === 0) || (seed % 5 === 0)) //big destiny
    return 3;
  else if ((seed % 17 === 0))  //insane destiny
    return 5;
  else if ((seed % 4 === 0) || (seed % 7 === 0)) //very big destiny
    return 4;
  else if (seed % 2 === 1) //somewhat big destiny
    return 2;
  else             //small destiny
    return 1;
}

const fetchUsername = async (id: string) => {
  if (usernameCache[id])
    return usernameCache[id];
  else {
    let response: any = await fetch(`https://discord.com/api/v10/users/${id}`, {
      headers: {
        Authorization: `Bot ${token}`
      }
    })

    response = await response.json();

    usernameCache[id] = response.username;
    return response.username ? response.username : 'Unknown';
  }
}

function sendMessage(id: string, msg: string) {
  client.users.fetch(id).then((user) => {
    user.send(msg)
  })
}

client.login(token);
