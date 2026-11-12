FROM node:24-alpine

WORKDIR /hizzasama

RUN apk add --no-cache chromium

ENV PUPPETEER_SKIP_DOWNLOAD=true \
    PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium-browser

COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

COPY src ./src
COPY tsconfig.json ./
RUN npm run build && npm prune --omit=dev

CMD ["node", "/hizzasama/src/hizzasama.js", "--refresh-commands"]
#CMD ["node", "/hizzasama/src/hizzasama.js"]
