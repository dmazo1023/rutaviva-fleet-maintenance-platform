#!/usr/bin/env node
import { readFileSync, existsSync } from 'node:fs'
import { resolve } from 'node:path'

const DEFAULTS = {
  base: 20000,
  tokensPerSkill: 8000,
  tokensPerReadLine: 12,
  tokensPerWrittenLine: 30,
  tokensPerTask: 2000,
  green: 60000,
  yellow: 90000,
  maxTasks: 4,
  maxFiles: 8,
  maxSkills: 1,
  maxFullReads: 6,
  fullReadMinLines: 100,
  maxWrittenLines: 900,
}

function usage() {
  console.error('Uso: node estimate-context.mjs --plan <plan.json> [--root <repo>] [--json]')
  process.exit(2)
}

function parseArgs(argv) {
  const opts = { root: process.cwd(), json: false }
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--plan') opts.plan = argv[++i]
    else if (argv[i] === '--root') opts.root = argv[++i]
    else if (argv[i] === '--json') opts.json = true
    else usage()
  }
  if (!opts.plan) usage()
  return opts
}

function countLines(root, entry) {
  if (typeof entry === 'object') return { label: entry.label ?? 'estimado', lines: entry.lines, full: false }
  const match = entry.match(/^(.*)#L(\d+)-L(\d+)$/)
  if (match) return { label: entry, lines: Number(match[3]) - Number(match[2]) + 1, full: false }
  const path = resolve(root, entry)
  if (!existsSync(path)) return { label: entry, lines: 0, full: true, missing: true }
  return { label: entry, lines: readFileSync(path, 'utf8').split('\n').length, full: true }
}

function estimate(agent, root, t) {
  const reads = (agent.read ?? []).map(e => countLines(root, e))
  const readLines = reads.reduce((sum, r) => sum + r.lines, 0)
  const tasks = agent.tasks ?? 0
  const skills = agent.skills ?? 0
  const writeLines = agent.writeLines ?? 0
  const files = (agent.files ?? []).length
  const tokens = t.base + skills * t.tokensPerSkill + readLines * t.tokensPerReadLine +
    writeLines * t.tokensPerWrittenLine + tasks * t.tokensPerTask
  const violations = []
  if (tasks > t.maxTasks) violations.push(`tareas ${tasks} > ${t.maxTasks}`)
  if (files > t.maxFiles) violations.push(`archivos ${files} > ${t.maxFiles}`)
  if (skills > t.maxSkills) violations.push(`skills ${skills} > ${t.maxSkills}`)
  if (writeLines > t.maxWrittenLines) violations.push(`líneas nuevas ${writeLines} > ${t.maxWrittenLines}`)
  const fullReads = reads.filter(r => r.full && r.lines > t.fullReadMinLines).length
  if (fullReads > t.maxFullReads) violations.push(`lecturas completas ${fullReads} > ${t.maxFullReads}`)
  const light = tokens <= t.green ? 'VERDE' : tokens <= t.yellow ? 'AMARILLO' : 'ROJO'
  const yellowAllowed = light === 'AMARILLO' && skills === 0
  const split = light === 'ROJO' || (light === 'AMARILLO' && !yellowAllowed) || violations.length > 0
  return {
    id: agent.id,
    tokens,
    light,
    verdict: split ? 'DIVIDIR' : 'OK',
    readLines,
    writeLines,
    tasks,
    files,
    skills,
    violations,
    missing: reads.filter(r => r.missing).map(r => r.label),
  }
}

const opts = parseArgs(process.argv.slice(2))
const plan = JSON.parse(readFileSync(resolve(opts.plan), 'utf8'))
const thresholds = { ...DEFAULTS, ...(plan.thresholds ?? {}) }
const results = (plan.agents ?? []).map(a => estimate(a, opts.root, thresholds))

if (opts.json) {
  console.log(JSON.stringify(results, null, 2))
} else {
  console.log('Agente\tTokens\tSemáforo\tVeredicto\tLeídas\tEscritas\tTareas\tArchivos\tSkills\tObservaciones')
  for (const r of results) {
    const notes = [...r.violations, ...r.missing.map(m => `no existe: ${m}`)].join('; ')
    console.log([r.id, r.tokens, r.light, r.verdict, r.readLines, r.writeLines, r.tasks, r.files, r.skills, notes].join('\t'))
  }
}

process.exit(results.some(r => r.verdict === 'DIVIDIR') ? 1 : 0)
