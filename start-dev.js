import { networkInterfaces } from 'node:os'
import { spawnSync } from 'node:child_process'

function getLocalIP() {
  for (const nets of Object.values(networkInterfaces())) {
    for (const net of nets) {
      if (net.family === 'IPv4' && !net.internal)
        return net.address
    }
  }
  return 'localhost'
}

const HOST_IP = getLocalIP()
console.log(`HOST_IP = ${HOST_IP}`)

spawnSync(
  'docker',
  ['compose', '-f', 'docker-compose.dev.yml', 'up', ...process.argv.slice(2)],
  { stdio: 'inherit', env: { ...process.env, HOST_IP } }
)
