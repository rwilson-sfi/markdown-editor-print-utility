import { copyFile, mkdir } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const projectDirectory = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const source = resolve(projectDirectory, 'node_modules', 'mermaid', 'dist', 'mermaid.min.js')
const destination = resolve(projectDirectory, 'dist', 'mermaid.min.js')

await mkdir(dirname(destination), { recursive: true })
await copyFile(source, destination)
