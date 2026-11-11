#!/usr/bin/env node
import { readFileSync, writeFileSync, existsSync, mkdirSync, appendFileSync } from 'node:fs'
import { join } from 'node:path'

const SUMMARY_REASON_LIMIT = 160

function usage() {
  console.error([
    'Uso: node persist-wave.mjs --output <salida del workflow> --harness <.claude/harness> --tasks <tasks.md> --wave <n>',
    '       [--run-id <wf_...>] [--append-owned <archivo con rutas de la ola siguiente>]',
  ].join('\n'))
  process.exit(2)
}

function parseArgs(argv) {
  const opts = {}
  for (let i = 0; i < argv.length; i += 2) {
    const key = argv[i]
    const value = argv[i + 1]
    if (!key?.startsWith('--') || value === undefined) usage()
    opts[key.slice(2)] = value
  }
  if (!opts.output || !opts.harness || !opts.tasks || !opts.wave) usage()
  return opts
}

function readResult(outputPath) {
  const raw = JSON.parse(readFileSync(outputPath, 'utf8'))
  const result = raw.result ?? raw
  return typeof result === 'string' ? JSON.parse(result) : result
}

function mergeHandoff(harness, wave, runId, result) {
  const path = join(harness, 'handoff.json')
  const handoff = existsSync(path) ? JSON.parse(readFileSync(path, 'utf8')) : { waves: {} }
  handoff.waves = handoff.waves ?? {}
  handoff.waves[wave] = {
    runId: runId ?? null,
    exports: result.handoff?.exports ?? {},
    decisions: result.handoff?.decisions ?? [],
  }
  writeFileSync(path, `${JSON.stringify(handoff, null, 2)}\n`)
  return Object.keys(handoff.waves[wave].exports).length
}

function markVerified(tasksPath, verified) {
  const lines = readFileSync(tasksPath, 'utf8').split('\n')
  const marked = []
  for (const id of verified) {
    const prefix = `- [ ] ${id} `
    const index = lines.findIndex(line => line.startsWith(prefix))
    if (index >= 0) {
      lines[index] = `- [x] ${id} ${lines[index].slice(prefix.length)}`
      marked.push(id)
    }
  }
  writeFileSync(tasksPath, lines.join('\n'))
  return marked
}

function appendOwned(harness, listPath) {
  const entries = readFileSync(listPath, 'utf8').split(/\r?\n/).map(l => l.trim()).filter(Boolean)
  const block = `${entries.join('\n')}\n`
  appendFileSync(join(harness, 'owned-files.txt'), block)
  const changeOwned = join(harness, 'change-owned-files.txt')
  if (existsSync(changeOwned)) appendFileSync(changeOwned, block)
  return entries.length
}

const opts = parseArgs(process.argv.slice(2))
const result = readResult(opts.output)
mkdirSync(join(opts.harness, 'waves'), { recursive: true })
writeFileSync(join(opts.harness, 'waves', `wave-${opts.wave}.result.json`), `${JSON.stringify(result, null, 2)}\n`)
const exportsCount = mergeHandoff(opts.harness, opts.wave, opts['run-id'], result)
const marked = markVerified(opts.tasks, result.verified ?? [])
const owned = opts['append-owned'] ? appendOwned(opts.harness, opts['append-owned']) : 0

const escalated = result.escalated ?? []
console.log(`Ola ${opts.wave}: verificadas ${marked.length}/${(result.verified ?? []).length} marcadas [${marked.join(', ')}]`)
console.log(`Escaladas: ${escalated.length}${escalated.length ? '' : ' (ninguna)'}`)
escalated.forEach(e => console.log(`  - ${e.taskId} (intentos ${e.attempts ?? 0}): ${String(e.reason).replace(/\s+/g, ' ').slice(0, SUMMARY_REASON_LIMIT)}`))
if ((result.unaccounted ?? []).length) console.log(`SIN CONTABILIZAR (defecto de la plantilla): ${result.unaccounted.join(', ')}`)
console.log(`Handoff: ${exportsCount} firmas guardadas${owned ? ` | owned-files: +${owned} rutas` : ''}`)
process.exit(escalated.length || (result.unaccounted ?? []).length ? 1 : 0)
