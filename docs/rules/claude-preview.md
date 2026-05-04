# Claude Preview — launch.json Setup

File `.claude/launch.json` cấu hình dev server cho `preview_start` tool.

## Cấu trúc cơ bản

```json
{
  "version": "0.0.1",
  "configurations": [
    {
      "name": "admin-dev",
      "runtimeExecutable": "...",
      "runtimeArgs": [...],
      "port": 5173,
      "autoPort": true,
      "cwd": "admin"
    }
  ]
}
```

## Cross-platform (Mac + Windows) — dùng Node

Node có sẵn trên cả 2 OS. `shell: true` tự resolve `pnpm` từ PATH của hệ điều hành — không cần hardcode path.

```json
{
  "version": "0.0.1",
  "configurations": [
    {
      "name": "admin-dev",
      "runtimeExecutable": "node",
      "runtimeArgs": ["-e", "require('child_process').spawn('pnpm',['dev'],{stdio:'inherit',shell:true})"],
      "port": 5173,
      "autoPort": true,
      "cwd": "admin"
    },
    {
      "name": "client-dev",
      "runtimeExecutable": "node",
      "runtimeArgs": ["-e", "require('child_process').spawn('pnpm',['dev'],{stdio:'inherit',shell:true})"],
      "port": 5174,
      "autoPort": true,
      "cwd": "client"
    }
  ]
}
```

## Mac only — absolute path (Homebrew)

```json
{
  "runtimeExecutable": "/opt/homebrew/bin/pnpm",
  "runtimeArgs": ["dev"]
}
```

Lỗi trên Windows vì path Homebrew không tồn tại.

## Mac only — shell wrapper

```json
{
  "runtimeExecutable": "/bin/sh",
  "runtimeArgs": ["-c", "PATH=/opt/homebrew/bin:$PATH pnpm dev"]
}
```

Dùng khi MCP không inherit PATH của user. Lỗi trên Windows vì `/bin/sh` không tồn tại.

## Windows only — cmd wrapper

```json
{
  "runtimeExecutable": "cmd.exe",
  "runtimeArgs": ["/c", "pnpm dev"]
}
```

`cmd.exe /c` tự resolve PATH của user. Lỗi trên Mac.

## Quyết định nhanh

| Tình huống | Dùng cách nào |
|-----------|---------------|
| Chỉ Mac | absolute path (`/opt/homebrew/bin/pnpm`) |
| Chỉ Windows | `cmd.exe /c pnpm dev` |
| Cả 2 OS | Node + `shell: true` |
