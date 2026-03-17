FROM node:20-alpine

WORKDIR /app

RUN apk add --no-cache dumb-init

COPY package*.json ./

RUN npm ci --only=production

COPY prisma ./prisma/
RUN npx prisma generate

COPY dist ./dist/

RUN mkdir -p uploads/reels uploads/reels/thumbnails logs

EXPOSE 3000

USER node

ENV NODE_ENV=production

ENTRYPOINT ["dumb-init", "--"]

CMD ["node", "dist/index.js"]
