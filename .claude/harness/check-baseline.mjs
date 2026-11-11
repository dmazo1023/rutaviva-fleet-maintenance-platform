#!/usr/bin/env node
import { readFileSync, existsSync } from 'node:fs'
import { createHash } from 'node:crypto'

const [baselinePath, ownedPath] = process.argv.slice(2)
if (!baselinePath || !ownedPath) {
  console.error('Uso: node check-baseline.mjs <baseline-hashes.txt> <change-owned-files.txt>')
  process.exit(2)
}

const owned = readFileSync(ownedPath, 'utf8').split(/\r?\n/).map(l => l.trim()).filter(l => l && !l.startsWith('#'))
const isOwned = file => owned.some(entry => entry.endsWith('/') ? file.startsWith(entry) : file === entry)

const violations = readFileSync(baselinePath, 'utf8').split(/\r?\n/).filter(Boolean).flatMap(line => {
  const separator = line.indexOf('  ')
  const hash = line.slice(0, separator)
  const file = line.slice(separator + 2)
  if (!existsSync(file)) return isOwned(file) ? [] : [`${file} (eliminado)`]
  const current = createHash('sha1').update(readFileSync(file)).digest('hex')
  return current !== hash && !isOwned(file) ? [`${file} (modificado)`] : []
})

if (violations.length) {
  console.log('VERIFY-WAVE FAILED [0-baseline]\nArchivos existentes modificados fuera de la asignación:\n' + violations.slice(0, 30).join('\n'))
  process.exit(1)
}
console.log('VERIFY-WAVE OK [0-baseline]')
