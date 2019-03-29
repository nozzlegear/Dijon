FROM node:10-alpine
WORKDIR /app

COPY package.json yarn.lock ./
RUN yarn install --frozen-lockfile --production false
COPY . .
RUN yarn build

ENTRYPOINT ["yarn", "start"]
