
//SYNC COMMANDS
import { REST, Routes, Client, GatewayIntentBits, Partials, ButtonBuilder, ButtonStyle, ActionRowBuilder, ButtonInteraction, ChatInputCommandInteraction, Interaction, EmbedBuilder } from 'discord.js';
import process from 'process';
import { Parser } from 'json2csv';
import csv from 'csvtojson'
import fs from 'fs'
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import fetch from "node-fetch";
import { Chess } from 'chess.js'
import { execSync } from 'child_process';
import request from 'request';
import {v4 as uuidv4} from 'uuid';
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

let chessOngoing = false;
let player1 = '';
let player2 = '';
let whitePlaying : boolean;

const gptGuilds = ["1249401431262232636", "1167198223832723476", "954741402586185778", "841363743957975063", "1278753068669993012"]
let temperature  = 1.0;
let max_tokens = 512;
let chatModel = 'gpt-4-turbo';
let imageModel = 'dall-e-3';
let behaviour = "";
let top_p = 1;

let coinClaimLuck = 0.15;

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
          type: 4
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
          type: 4
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
          type: 4
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
      name: 'jojoref',
      description: 'Give me a random quote from JoJo parts 1 through 5'
    },
    {
      name: 'destiny',
      description: "I want to know today's destiny."
    },
    {
      name: 'counter',
      description: "Keep count of this for me",
      options: [
        {
          name: "name",
          description: "What is the name of the counter",
          required: true,
          type: 3
        },
        {
          name: "increment",
          description: "Whether to increment the counter or not",
          required: false,
          type: 5
        }
      ]
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
    }

  ];

  const rest = new REST({ version: '10' }).setToken(token!);
  (async () => {
    try {
      console.log('Started refreshing application (/) commands.');

      await rest.put(Routes.applicationCommands("1392080379862712472"), { body: commands });

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

 fs.writeFileSync(`${__dirname}/activity.log`, Date.now() + ", " + newPresence.userId + ", " + status + "\n", {
   encoding: "utf8",
   flag: "a+",
   mode: 0o666
 }); 
})

client.on("messageCreate", async (message : any) => {
  fetch("https://andrewbuhagiar.com:8443/counter/89c63b4d-bd43-4b85-b8a5-6d0a10b18263");

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
      else if (message.author.id === "586278312809201665")
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
    case "jojoref":           try { await jojoRef(interaction); }         catch (err) { console.error(err) } break;
    case "destiny":           try { await destiny(interaction); }         catch (err) { console.error(err) } break;
    case "tell":              try { await tell(interaction); }            catch (err) { console.error(err) } break;
    case "imagine":           try { await imagine(interaction); }            catch (err) { console.error(err) } break;
    case "counter":           try { await counter(interaction); }         catch (err) { console.error(err) } break;
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
        responseText += `\`+${response.Streak}\` Streak\n`;
      if(response.ClaimedReward.RewardedAmount > 0)
        responseText += `\`+${response.ClaimedReward.RewardedAmount}\` Reward for \`${response.ClaimedReward.Streak}\` Streak\n`;
      if(response.Multiplier > 1)
        responseText += `\`x${response.Multiplier}\` MULTIPLIER! 🪙🪙🪙\n\n`;

      responseText += `TOTAL COIN CLAIMED: \`${response.TotalClaim}\` 🪙\n`
      responseText += `Next Reward is at Streak Day \`${response.NextReward.Streak}\``
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
      responseText += ` (\`${response.WageredBalance}\` of which has been wagered)`
    responseText = `${interaction.options!.get('person') ? '<@'+interaction.options!.get('person')!.user!.id! + '> has' : 'You have'} \`${response.Balance}\` HizzaCoin 🪙`;

    await interaction.reply(responseText);
  }
}

export async function coinLeaderboard(interaction: ChatInputCommandInteraction) {
  if(interaction){  
    const response : Account[] = await (await fetch(`http://localhost:8080/api/coin-commands/coin-leaderboard`)).json();
    console.log(response);
    await interaction.deferReply();
    
    let leaderboardText = "**...................  LeaderBoard  ....................**\n";
    for(let i = 0; i < response.length; i++){
      let username = await fetchUsername(response[i].DiscordId);
      leaderboardText += `${i+1}) **${username.padEnd(15, " ")}** with **${response[i].Balance.toString().padStart(5, " ")}** HizzaCoin\n`;
    };
    
    await interaction.editReply(leaderboardText);
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
    const response = await fetch(`http://localhost:8080/api/coin-commands/coin-give?
                                senderDiscordId=${interaction.user.id}
                                &receiverDiscordId=${interaction.options!.get('payee')!.user!.id}
                                &amountToSend=${parseInt((interaction.options!.get('amount')!.value)!.toString())}`);
    await interaction.reply(`\`\`\`json\n${await response.text()}\`\`\``);
  }
}
export async function challenge(interaction: ChatInputCommandInteraction) {
  if(interaction){
    const challengeId = uuidv4();
    const emotes : {[key: string] : string} = {"rock": "🪨", "paper": "📰", "scissors": "✂️"};
    
    //Challenge initiated logic
    const opponent = interaction.options.get('opponent');
    
    if(opponent!.user!.id === botId){
      await interaction.reply({
        content: `You cannot challenge me!`,
        ephemeral: true
      });

      return;
    }

    const wager : number | null = interaction.options!.get('wager') ? parseInt((interaction.options!.get('wager')!.value)!.toString()) : null;

    if(wager){
      //Ensure wager is valid
      if(wager != null && true){
        await interaction.reply(`Wagers are disabled until HIZZACOIN 3.0 fully comes out some time in the next few weeks!`);
        return;
      }

      //Ensure the challengee can wager that much
      if(!await addHizzaCoin(interaction.user.id, -wager, true))
      {
        await interaction.reply({
          content: `You cannot wager that much HizzaCoin! ❌🍕`,
          ephemeral: true
        });
        
        return;
      }

      //if the opponent can also wager that much
      if(!await addHizzaCoin(opponent!.user!.id, -wager, false)){
        await addHizzaCoin(interaction.user.id, wager, true);
        await interaction.reply({
          content: `<@${opponent!.user!.id}> cannot wager that much HizzaCoin! ❌🍕`,
          ephemeral: true
        });
        
        return;
      }
    }

    //check that player did not challenge himself
    if (interaction.user.id === opponent!.user!.id) {
      await interaction.reply({
        content: `You cannot challenge yourself! ❌`,
        ephemeral: true
      });
      
      return;
  }
    
    const rock = new ButtonBuilder()
    .setCustomId(`rock:${challengeId}`)
    .setLabel('Rock')
    .setStyle(ButtonStyle.Primary)
    .setEmoji('🪨');

    const paper = new ButtonBuilder()
    .setCustomId(`paper:${challengeId}`)
    .setLabel('Paper')
    .setStyle(ButtonStyle.Primary)
    .setEmoji('📰');

    const scissors = new ButtonBuilder()
    .setCustomId(`scissors:${challengeId}`)
    .setLabel('Scissors')
    .setStyle(ButtonStyle.Primary)
    .setEmoji('✂️');

    const responseRow : any = new ActionRowBuilder()
    .addComponents(rock, paper, scissors);

    await interaction.reply({
      content: `${wager ? '🪙🪙🪙' : ''} <@${opponent!.user!.id}> has been challenged by <@${interaction.user.id}>` + (wager ? ` with a **${wager} HizzaCoin wager** 🪙🪙🪙!` : `!`),
      components: [responseRow],
      ephemeral: false
    });

    const timeout = 900000;
    const buttonCollector = interaction.channel!.createMessageComponentCollector({ time: timeout });
    
    let winner = -1;
    let answers : {[key: string] : string} = {};
    let lock = false;

    //Refund if time expires
    setTimeout(async () => {
      if(wager && !lock){
        //Refund challenger always as they put deposit immediately
        await addHizzaCoin(interaction.user.id, wager, true);

        //Refund opponent maybe because they only get charged on reaction
        if(answers[opponent!.user!.id])
          await addHizzaCoin(opponent!.user!.id, wager, true);        

        buttonCollector.stop();
        lock = false;
      }
    }, timeout)

    //THIS IS SUPER NOT OPTIMAL. LETS HOPE IT DOESN'T GET POPULAR
    buttonCollector.on('collect', async (buttonInteraction : ButtonInteraction) => {
      if(buttonInteraction.customId.split(':')[1] !== challengeId)
        return;

      const interactor = buttonInteraction.user.id;

      if(answers[interactor]){
        await buttonInteraction.reply({
          content: `You already chose your hand!`,
          ephemeral: true
        });
        return;
      }

      //Ensure user is valid
      if(interactor !== interaction.user.id && interactor !== opponent!.user!.id){
        await buttonInteraction.reply({
          content: `You are not part of this rock paper scissors match!`,
          ephemeral: true
        });
        return;
      }else{
        answers[interactor] = buttonInteraction.customId.split(':')[0];
        if (Object.keys(answers).length === 1)
          await buttonInteraction.reply({
            content: `You chose your hand as ${buttonInteraction.customId.split(':')[0]}!`,
            ephemeral: true
          })
      }

      //Take money from opponent if it is the opponent reacting
      if(interactor === opponent!.user!.id){
        if(wager && !(await addHizzaCoin(opponent!.user!.id, -wager, true))){
          await addHizzaCoin(interaction.user.id, wager, true);
          buttonCollector.stop();
          await buttonInteraction.reply({
            content: `The opponent did not have enough HizzaCoin to match the wager!`,
            ephemeral: true
          });
          return;
        }
      }

      //Calculate winner if both parties did their hand  
      if (Object.keys(answers).length === 2) {
        buttonCollector.stop();
        switch (answers[interaction.user.id]) {
            case 'rock':
                switch (answers[opponent!.user!.id]) {
                    case 'paper': winner = 2; break;
                    case 'scissors': winner = 1; break;
                    default: winner = 0;
                }
                break;

            case 'paper':
                switch (answers[opponent!.user!.id]) {
                    case 'rock': winner = 1; break;
                    case 'scissors': winner = 2; break;
                    default: winner = 0;
                }
                break;

            case 'scissors':
                switch (answers[opponent!.user!.id]) {
                    case 'rock': winner = 2; break;
                    case 'paper': winner = 1; break;
                    default: winner = 0;
                }
                break;
        }
        //declare winner and see if any Hizza Coin was wagered
        if (winner !== 0) {
            const winnerPlayer = winner === 1 ? interaction.user.id : opponent!.user!.id;   
            const loserPlayer = winner === 1 ? opponent!.user!.id : interaction.user.id;

            let winMsg = `<@${winnerPlayer}> ${emotes[answers[winnerPlayer]]} beat <@${loserPlayer}> ${emotes[answers[loserPlayer]]}!`
            if(wager){
              addHizzaCoin(winnerPlayer, wager * 2, true)
              winMsg = winMsg + `\n And has won ${wager} HizzaCoin! 🪙`      
            }
            if(!lock)
              await buttonInteraction.reply(winMsg)

            lock = true;
        }
        else{
            await buttonInteraction.reply(`<@${interaction.user.id}> ${emotes[answers[interaction.user.id]]} tied with <@${opponent!.user!.id}> ${emotes[answers[opponent!.user!.id]]}!`)
            if(wager){
                await addHizzaCoin(interaction.user.id, wager, true)
                await addHizzaCoin(opponent!.user!.id, wager, true)
            }
        }
    }     
    });
  }else{
    return;
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


export async function counter(interaction: ChatInputCommandInteraction){
  if(interaction){
    const counterName = interaction.options.get('name')!.value?.toString()!;
    if(counterName.length < 0 && counterName.length > 36){
      await interaction.reply(`Counter name is too long! (not more than 36 characters)`);
      return;
    }

    const increment = interaction.options.get('increment');
    const response = await fetch(`https://andrewbuhagiar.com:8443/counter/${counterName}?increment=${increment ? increment.value : true}`);

    await interaction.reply(`Counter \`${counterName}\` is at \`${await response.text()}\``);
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

export async function jojoRef(interaction: ChatInputCommandInteraction) {
  if(interaction){
    fs.readFile(`${__dirname}/../resources/jojo.txt`, 'utf8', async (err, data) => {
      try {
          let quotes = data.split("\n")
          let size = quotes.length;
          let randomQuoteNo = Math.floor(Math.random() * size);
          await interaction.reply(quotes[randomQuoteNo])
      } catch {  }
    })
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

const compareHizzaCoin = (a: { amount: number }, b: { amount: number }) => b.amount - a.amount;

async function addHizzaCoin(userId: string, amount: number, commitTransaction: boolean) {
  let newUser = true;

  //get all files and check if user already has a hizzacoin account
  const bankDir = `${__dirname}/../resources/coinBank.csv`;
  const hizzaAccounts = await csv().fromFile(bankDir);
  for (const hizzaAccount of hizzaAccounts) {
    //if user is found change values
    if (hizzaAccount.userID === userId) {
      newUser = false;
      try {
        //calculate new balance check if it is valid
        if (0 > parseFloat(hizzaAccount.amount) + amount)
          return false;

        //set new balance
        if(commitTransaction){
          hizzaAccount.amount = parseFloat(hizzaAccount.amount) + amount;

          //save balance and other information
          const csvToSave = new Parser({ fields: ["userID", "amount", "date", "streak"] }).parse(hizzaAccounts);
          fs.writeFileSync(bankDir, csvToSave);
        }
        return true;
      } catch (err) {
        console.error(err);
      }
    }
  }
  if (newUser === true) {
    //user doesnt have a HizzaCoin account. They can use 'coin claim' in a server with me to get some
    return false;
  }
  //hizza coin changes successful
  return true;
}

function sendMessage(id: string, msg: string) {
  client.users.fetch(id).then((user) => {
    user.send(msg)
  })
}

async function coinTransaction(userFrom: string, userTo: string, amount: number) {
  let hasEnough = await addHizzaCoin(userFrom, -amount, true);
  if (hasEnough === true) {
    if (await addHizzaCoin(userTo, amount, true) === true)
      return true;
    else {
      await addHizzaCoin(userFrom, amount, true)
      sendMessage(userFrom, "You have been refunded as the person you sent money to does not have a HizzaCoin Account 😥")
      return false;
    }
  }
  return false;
}

client.login(token);
