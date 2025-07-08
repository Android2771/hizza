# Hizza-Sama Discord Bot
<img src="resources/hizza.png" alt="drawing" width="300"/>

## Required Bot Token
Make sure to place the required openai and discord tokens in the .env file as follows:
```
DISCORD_BOT_TOKEN='<discord_token>'
OPEN_AI_KEY='<openai_token>'
```

## How to Install Hizza (Docker)
To install docker:
```
curl -fsSL https://get.docker.com -o get-docker.sh
chmod +x get-docker.sh
sudo ./get-docker.sh
rm get-docker.sh
```

To build and run hizza with python modules:
```
sudo docker compose up -d
```

## How to Install Hizza (Manually)
* `npm install` to install the required packages
* `tsc` to compile hizza, or `tsc --watch` to compile everytime `hizzasama.ts` changes

## How to Run (Manually)
* `node hizzasama.js` to run the bot normally

## How to Use
* After inviting the bot to your server, try executing the `commands` command
* The bot can also read dm's. You can read dm's in the `resources/dms` folder, where all the users interacting with the bot are organised into files.
* You can send dm's by using the `sendMessage()` and `sendAll()` functions provided in the code. Note that the `sendAll()` function requires a file `resources/ids.txt` containing the ids of all users in the server (this is to limit dm spam)


