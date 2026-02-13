FROM node:20-alpine

WORKDIR /app

# copy package files trước để cache
COPY package.json package-lock.json* pnpm-lock.yaml* yarn.lock* ./

# install deps (tự chọn theo project)
RUN pnpm install
# RUN yarn install

# copy source
COPY . .

EXPOSE 5173

CMD ["npm", "run", "dev", "--", "--host"]
