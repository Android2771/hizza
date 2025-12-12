FROM node:alpine

WORKDIR /hizzasama

COPY src ./src
COPY .env .
COPY package.json .
COPY package-lock.json .
COPY tsconfig.json .

RUN npm install
RUN npm install typescript -g
RUN tsc
RUN apk add chromium
RUN apk add tzdata
RUN cp /usr/share/zoneinfo/Europe/Malta /etc/localtime
RUN echo "Europe/Malta" | tee /etc/timezone

CMD ["node", "/hizzasama/src/hizzasama.js", "--refresh-commands"]
#CMD ["node", "/hizzasama/src/hizzasama.js"]
