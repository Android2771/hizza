FROM node:alpine

WORKDIR /hizzasama

COPY src ./src
COPY .env .
COPY package.json .
COPY package-lock.json .
COPY tsconfig.json .

RUN apk add chromium
RUN npm install
RUN npm install typescript -g
RUN tsc

#CMD ["node", "/hizzasama/src/hizzasama.js", "--refresh-commands"]
CMD ["node", "/hizzasama/src/hizzasama.js"]
