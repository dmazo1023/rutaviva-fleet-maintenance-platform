export const meta = {
  name: 'openspec-wave',
  description: 'Implement one OpenSpec wave in parallel, gate it, verify it and retry failed tasks',
  whenToUse: 'One wave of an OpenSpec change planned with satrack-openspec-parallel-harness',
  phases: [
    { title: 'Implement', detail: 'wave agents in parallel' },
    { title: 'Gate', detail: 'verify-wave.sh' },
    { title: 'Verify', detail: 'one verifier for the whole wave' },
    { title: 'Fix', detail: 'bounded retries per failed task; split per task on context exhaustion' },
  ],
}

const IMPLEMENTATION_SCHEMA = {
  type: 'object',
  properties: {
    status: { type: 'string', enum: ['complete', 'partial', 'blocked'] },
    tasks: { type: 'array', items: { type: 'string' } },
    files: { type: 'array', items: { type: 'string' } },
    exports: {
      type: 'array',
      items: {
        type: 'object',
        properties: { file: { type: 'string' }, signatures: { type: 'string' } },
        required: ['file', 'signatures'],
      },
    },
    decisions: { type: 'array', items: { type: 'string' } },
    blockers: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string' },
  },
  required: ['status', 'tasks', 'files'],
}

const GATE_SCHEMA = {
  type: 'object',
  properties: {
    passed: { type: 'boolean' },
    failedCheck: { type: 'string' },
    summary: { type: 'string' },
  },
  required: ['passed', 'summary'],
}

const VERDICT_SCHEMA = {
  type: 'object',
  properties: {
    results: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          taskId: { type: 'string' },
          passed: { type: 'boolean' },
          reason: { type: 'string' },
        },
        required: ['taskId', 'passed', 'reason'],
      },
    },
  },
  required: ['results'],
}

const input = typeof args === 'string' ? JSON.parse(args) : args
const VERIFY_ONLY = input.verifyOnly === true
const MAX_RETRIES = VERIFY_ONLY ? 0 : (input.maxRetries ?? 2)
const INFRASTRUCTURE_CHECKS = ['0-baseline', '1-ownership']
const NOTES_LIMIT = 600
const DECISION_LIMIT = 200
const MAX_DECISIONS = 3
const SIGNATURE_LINES_LIMIT = 15
const agents = input.agents
const handoff = input.handoff ?? { waves: {} }
const PRESERVE_RULE = 'Conserva todo el contenido previo de los archivos que modificas (comentarios existentes, encabezados generados como el esquema de ResX, espacios y finales de línea) y cambia solo las líneas que exige la tarea. Las reglas de estilo aplican al código nuevo; la compuerta solo revisa líneas agregadas.'
const PROTOCOL = [
  'Protocolo de comunicación:',
  '- No te comunicas con otros agentes. Lo que necesites de ellos lo recibes aquí como firma o por un nombre fijado en design.md.',
  '- Usa los tipos de otros agentes por su nombre fijo; nunca crees ni edites un archivo que no esté en tu lista para desbloquearte. Si la tarea exige un archivo nuevo que no está en tu lista, repórtalo en blockers: nunca metas esa clase en otro archivo tuyo ni edites manifiestos de propiedad para esquivar la compuerta.',
  '- Los contratos de esta misma ola los crea otro agente en paralelo y pueden no existir todavía en el árbol: úsalos por su nombre fijo y no los reportes como bloqueo. La compuerta compila todo junto al final.',
  '- Solo si un contrato falta en la especificación o es ambiguo, anótalo en blockers, completa lo demás y devuelve status "blocked".',
  '- Lee un archivo ajeno solo si la firma no basta, y por rango de líneas. Para una referencia como design.md#D4, lee solo esa sección.',
  '- Si notas que tu contexto no alcanza para terminar, guarda lo hecho y devuelve status "partial" con lo pendiente en notes.',
  `- Devuelve en exports las firmas públicas de los tipos que creaste o cambiaste (máximo ${SIGNATURE_LINES_LIMIT} líneas por archivo), en decisions como máximo ${MAX_DECISIONS} elecciones que afecten a otros agentes (${DECISION_LIMIT} caracteres cada una) y notes de máximo ${NOTES_LIMIT} caracteres.`,
].join('\n')

function cap(text, limit) {
  const value = text ?? ''
  return value.length > limit ? `${value.slice(0, limit)}…` : value
}

function capLines(text, limit) {
  return (text ?? '').split('\n').slice(0, limit).join('\n')
}

function normalize(path) {
  return path.replace(/\\/g, '/')
}

function canonicalFile(file) {
  const root = normalize(input.repoRoot ?? '').replace(/\/$/, '').toLowerCase()
  let value = normalize(file).replace(/^\.\//, '')
  if (root && value.toLowerCase().startsWith(`${root}/`)) value = value.slice(root.length + 1)
  const owned = agents.flatMap(a => a.files).find(o => value === o || value.endsWith(`/${o}`) || o.endsWith(`/${value}`))
  return owned ?? value
}

function failureSignature(gate) {
  const summary = (gate?.summary ?? '').replace(/\d+(\.\d+)?\s*m?s\b/g, '').replace(/\s+/g, ' ').trim()
  return `${gate?.failedCheck ?? ''}|${summary}`
}

function taskList(tasks) {
  return tasks.map(t => {
    const refs = t.refs?.length ? `\n  Referencias: ${t.refs.join(', ')}` : ''
    return `- ${t.id}: ${t.text}\n  Verificación: ${t.verify}${refs}`
  }).join('\n')
}

function knownExports() {
  const merged = {}
  Object.values(handoff.waves ?? {}).forEach(w => Object.assign(merged, w.exports ?? {}))
  return merged
}

const previousExports = knownExports()

function consumedSignatures(a) {
  return (a.consumes ?? []).map(file => {
    const signatures = previousExports[normalize(file)]
    return signatures ? `${file}:\n${signatures}` : `${file}: (sin firma en handoff; lee el archivo por rango si la necesitas)`
  })
}

const COMMON_BLOCK = [
  `Change OpenSpec "${input.change}", ola ${input.wave}.`,
  `Reglas: ${input.rules}`,
  PRESERVE_RULE,
  PROTOCOL,
  'No compiles ni ejecutes tests: la compuerta lo hace. No hagas commits ni crees ramas.',
  'En tasks devuelve solo los IDs exactos de las tareas completadas (por ejemplo "4.7"). En files devuelve la lista exacta de archivos que tocaste, con rutas relativas a la raíz del repositorio y separador /.',
].join('\n\n')

function agentBlock(a, tasks) {
  const signatures = consumedSignatures(a)
  return [
    `Rol: ${a.role}. Agente ${a.id}.`,
    `Tareas asignadas:\n${taskList(tasks)}`,
    `Archivos que puedes crear o modificar (y ningún otro; una ruta terminada en / autoriza la carpeta):\n${a.files.join('\n')}`,
    signatures.length ? `Firmas de los contratos que consumes (no los modifiques):\n${signatures.join('\n')}` : '',
    a.contracts ? `Contratos de esta misma ola que usas por nombre fijo:\n${a.contracts.join('\n')}` : '',
    a.skill ? `Invoca la skill ${a.skill} con la herramienta Skill cuando diseñes el código.` : '',
  ].filter(Boolean).join('\n\n')
}

function implementationPrompt(a, tasks = a.tasks) {
  return `${COMMON_BLOCK}\n\n${agentBlock(a, tasks)}`
}

function ownsFile(agent, file) {
  return agent.files.some(owned => owned.endsWith('/') ? file.startsWith(owned) : file === owned)
}

function ownerOf(taskId) {
  return agents.find(a => a.tasks.some(t => t.id === taskId))
}

function taskById(taskId) {
  return agents.flatMap(a => a.tasks).find(t => t.id === taskId)
}

function outsideAssignment(agent, result) {
  return (result?.files ?? []).map(canonicalFile).filter(f => !ownsFile(agent, f))
}

function reportedTask(result, taskId) {
  return (result?.tasks ?? []).some(r => r.trim() === taskId || r.trim().startsWith(`${taskId} `))
}

function agentsBlamedBy(summary) {
  const text = normalize(summary ?? '')
  return agents.filter(a => a.files.some(owned => text.includes(owned.endsWith('/') ? owned : owned.split('/').pop())))
}

const verified = new Set()
const escalated = new Map()
const exhausted = new Set()
const lastNotes = new Map()
const waveExports = {}
const waveDecisions = []
const allTaskIds = agents.flatMap(a => a.tasks.map(t => t.id))
const attempts = new Map(allTaskIds.map(id => [id, 0]))
const open = id => !verified.has(id) && !escalated.has(id)

function absorb(agentId, taskIds, result) {
  if (!result || result.status === 'partial') {
    taskIds.forEach(id => exhausted.add(id))
    if (result?.notes) lastNotes.set(agentId, cap(result.notes, NOTES_LIMIT))
    return
  }
  taskIds.forEach(id => exhausted.delete(id))
  if (result.notes) lastNotes.set(agentId, cap(result.notes, NOTES_LIMIT))
  ;(result.exports ?? []).forEach(e => { waveExports[canonicalFile(e.file)] = capLines(e.signatures, SIGNATURE_LINES_LIMIT) })
  ;(result.decisions ?? []).slice(0, MAX_DECISIONS).forEach(d => waveDecisions.push(cap(d, DECISION_LIMIT)))
  if (result.status === 'blocked') {
    const reason = `Bloqueada: ${cap((result.blockers ?? []).join(' | '), NOTES_LIMIT)}`
    taskIds.filter(id => !reportedTask(result, id)).forEach(id => escalated.set(id, { taskId: id, reason }))
    log(`Agente ${agentId} bloqueado: ${reason}`)
  }
}

async function runGate(attempt) {
  return agent(
    `Desde la raíz del repositorio ${input.repoRoot}, ejecuta exactamente este comando con la herramienta Bash (timeout 600000 ms) y reporta el resultado sin modificar archivos:\n${input.gateCommand}\n` +
    'No uses run_in_background ni Monitor. Si la herramienta mueve el comando al segundo plano, espera su notificación de fin y lee la salida completa antes de responder. Nunca respondas con un resultado incompleto: el veredicto exige el código de salida final.\n' +
    'passed=true solo si el código de salida es 0. En failedCheck pon el nombre del chequeo que falló (por ejemplo 4-build). En summary copia como máximo 30 líneas del error, con las rutas de archivo tal como aparecen. Si falla un test y el error no trae la ruta de su archivo, busca con Grep el nombre del método del test y agrega al summary la ruta relativa del archivo que lo contiene, para que se reactive solo al agente dueño.',
    { label: `gate:${attempt}`, phase: 'Gate', schema: GATE_SCHEMA, model: 'haiku', effort: 'low' })
}

async function verifyWave(taskIds) {
  if (!input.verifyByAgent) return verifyGroup(taskIds, agents, `wave-${input.wave}`)
  const groups = agents.filter(a => a.tasks.some(t => taskIds.includes(t.id)))
  const verdicts = await parallel(groups.map(a => () => verifyGroup(taskIds, [a], `wave-${input.wave}:${a.id}`)))
  return { results: verdicts.filter(Boolean).flatMap(v => v.results ?? []) }
}

async function verifyGroup(taskIds, group, label) {
  const tasks = group.flatMap(a => a.tasks).filter(t => taskIds.includes(t.id))
  const files = group.flatMap(a => a.files)
  const decisions = waveDecisions.length ? `\nDecisiones declaradas por los agentes (comprueba que no contradigan design.md):\n${waveDecisions.map(d => `- ${d}`).join('\n')}\n` : ''
  return agent(
    `Verifica las tareas de la ola ${input.wave} del change "${input.change}" contra su criterio.\n` +
    `${taskList(tasks)}\n\nRevisa solo el diff (git diff y archivos nuevos sin versionar) de estos archivos:\n${files.join('\n')}\n` +
    'Los archivos ignorados por git (por ejemplo local.settings.json) léelos directamente. La compuerta ya confirmó que compila y que los tests pasan con la línea base; evalúa lo que el script no comprueba (criterio funcional, cobertura de los casos pedidos en los tests, constantes, reglas de diseño). Las partes de un criterio que exigen BD de test, credenciales o arrancar un servicio en local no las evalúes: aprueba si el código está listo y dilo en reason.\n' +
    decisions +
    `\nReglas a comprobar además del criterio: ${input.rules}\n${PRESERVE_RULE} Rechaza la tarea si el diff borra contenido previo que la tarea no pedía cambiar.\n` +
    'Lee por rangos y solo lo necesario para cada criterio. Invoca la skill satrack-sonarqube-quality-gates si necesitas criterios de calidad. No modifiques archivos. ' +
    'Marca passed=false ante la duda y explica el motivo concreto (archivo y qué falta) en reason. Devuelve un resultado por cada tarea listada.',
    { label: `verify:${label}`, phase: 'Verify', schema: VERDICT_SCHEMA, model: input.verifierModel ?? 'sonnet', effort: 'medium' })
}

function fixPrompt(owner, items, split) {
  const tasks = items.map(i => taskById(i.taskId)).filter(Boolean)
  const previous = lastNotes.get(owner.id)
  return [
    implementationPrompt(owner, tasks),
    split
      ? 'Un agente anterior se quedó sin contexto con estas tareas. Ahora tienes una sola: parte del árbol actual, lee solo los archivos de esta tarea y no rehagas lo que ya está bien.'
      : 'Corrige estos fallos. El trabajo previo ya está en el árbol: parte de él, no lo rehagas desde cero.',
    previous ? `Notas del intento anterior: ${previous}` : '',
    items.map(i => `- ${i.taskId} (intento ${attempts.get(i.taskId)} de ${MAX_RETRIES}): ${i.reason}`).join('\n'),
  ].filter(Boolean).join('\n\n')
}

async function fixTasks(failures, round) {
  const jobs = []
  const byOwner = new Map()
  for (const f of failures) {
    const owner = ownerOf(f.taskId) ?? agents[0]
    if (exhausted.has(f.taskId)) jobs.push({ owner, items: [f], split: true })
    else byOwner.set(owner, [...(byOwner.get(owner) ?? []), f])
  }
  byOwner.forEach((items, owner) => jobs.push({ owner, items, split: false }))
  const results = await parallel(jobs.map(job => () =>
    agent(fixPrompt(job.owner, job.items, job.split), {
      label: `fix:${job.owner.id}${job.split ? `:${job.items[0].taskId}` : ''}:r${round}`,
      phase: 'Fix', schema: IMPLEMENTATION_SCHEMA, model: job.owner.model, effort: job.owner.effort,
    })))
  jobs.forEach((job, i) => absorb(job.owner.id, job.items.map(it => it.taskId), results[i]))
}

phase('Implement')
if (VERIFY_ONLY) log('Modo verifyOnly: sin implementación ni correcciones; solo compuerta y verificador')
const implemented = VERIFY_ONLY
  ? agents.map(a => ({ status: 'complete', tasks: a.tasks.map(t => t.id), files: [] }))
  : await parallel(agents.map(a => () =>
    agent(implementationPrompt(a), { label: `impl:${a.id}`, phase: 'Implement', schema: IMPLEMENTATION_SCHEMA, model: a.model, effort: a.effort })))

const initialFailures = new Map()
agents.forEach((a, i) => {
  const result = implemented[i]
  const taskIds = a.tasks.map(t => t.id)
  absorb(a.id, taskIds, result)
  const intruded = outsideAssignment(a, result)
  intruded.forEach(f => log(`Agente ${a.id} tocó un archivo fuera de su asignación: ${f}`))
  if (!result) log(`Agente ${a.id} no devolvió resultado (contexto agotado o fallo terminal); sus tareas se reintentan una por agente`)
  a.tasks.filter(t => open(t.id)).forEach(t => {
    const reason = !result ? 'El agente no terminó (posible contexto agotado)'
      : result.status === 'partial' ? `El agente se quedó sin contexto: ${cap(result.notes, NOTES_LIMIT)}`
      : intruded.length ? `Archivos fuera de la asignación: ${intruded.join(', ')}`
      : !reportedTask(result, t.id) ? 'El agente no reportó la tarea como completada'
      : null
    if (reason) initialFailures.set(t.id, { taskId: t.id, reason })
  })
})

let pending = [...initialFailures.values()]
let round = 0
let previousFailure = null
const history = []

function escalateOpen(reason) {
  allTaskIds.filter(open).forEach(id => escalated.set(id, { taskId: id, reason }))
}

while (true) {
  pending = pending.filter(p => open(p.taskId))
  pending.filter(p => attempts.get(p.taskId) >= MAX_RETRIES).forEach(p => escalated.set(p.taskId, p))
  const retry = pending.filter(p => open(p.taskId))
  if (pending.length && !retry.length) break
  if (retry.length) {
    round++
    retry.forEach(p => attempts.set(p.taskId, attempts.get(p.taskId) + 1))
    log(`Ronda de corrección ${round}: ${retry.map(p => `${p.taskId} (${attempts.get(p.taskId)}/${MAX_RETRIES}${exhausted.has(p.taskId) ? ', dividida' : ''})`).join(', ')}`)
    await fixTasks(retry, round)
  }

  if (!allTaskIds.some(open)) break

  const stillExhausted = allTaskIds.filter(id => open(id) && exhausted.has(id))
    .map(id => ({ taskId: id, reason: 'El agente de corrección también se quedó sin contexto' }))

  const gate = await runGate(round)
  history.push({ round, gate: { passed: gate?.passed ?? false, failedCheck: gate?.failedCheck ?? null } })
  if (!gate?.passed) {
    const blamed = agentsBlamedBy(gate?.summary)
    const reason = `Compuerta fallida (${gate?.failedCheck ?? 'desconocido'}): ${gate?.summary ?? ''}`
    const signature = failureSignature(gate)
    if (!blamed.length && INFRASTRUCTURE_CHECKS.includes(gate?.failedCheck)) {
      escalateOpen(`Falla de infraestructura, sin reintentos: el error no apunta a ningún archivo asignado. ${reason}`)
      log(`Falla de infraestructura (${gate?.failedCheck}); la ola se detiene sin gastar reintentos`)
      break
    }
    if (previousFailure !== null && signature === previousFailure) {
      escalateOpen(`Falla no convergente, sin más reintentos: la compuerta falla igual después de una ronda de corrección (probable causa de infraestructura, entorno o diseño). ${reason}`)
      log('La compuerta repite el mismo fallo tras la corrección; la ola se detiene para que el orquestador diagnostique')
      break
    }
    previousFailure = signature
    const scope = blamed.length ? blamed : agents
    const gateFailures = scope.flatMap(a => a.tasks.map(t => t.id)).filter(id => open(id) && !exhausted.has(id)).map(id => ({ taskId: id, reason }))
    pending = [...gateFailures, ...stillExhausted]
    if (!pending.length) {
      escalateOpen(`Bloqueada: la compuerta falla por tareas ya escaladas y no queda ninguna tarea a la que reintentar. ${reason}`)
      log('La compuerta falla por tareas ya escaladas; la ola se detiene sin repetir la compuerta')
      break
    }
    log(`Compuerta fallida (${gate?.failedCheck ?? 'desconocido'}); tareas afectadas: ${pending.map(p => p.taskId).join(', ')}`)
    continue
  }

  const toVerify = allTaskIds.filter(id => open(id) && !exhausted.has(id))
  const verdict = toVerify.length ? await verifyWave(toVerify) : { results: [] }
  const results = verdict?.results ?? []
  history.push({ round, verdict: results.map(r => ({ taskId: r.taskId, passed: r.passed })) })
  results.filter(r => r.passed).forEach(r => verified.add(r.taskId))
  pending = [
    ...toVerify.filter(id => !verified.has(id))
      .map(id => ({ taskId: id, reason: results.find(r => r.taskId === id)?.reason ?? 'Sin veredicto del verificador' })),
    ...stillExhausted,
  ]
  if (!pending.length) break
}

allTaskIds.filter(open).forEach(id => escalated.set(id, {
  taskId: id,
  reason: 'Sin verificar: la ola se detuvo porque otras tareas agotaron sus reintentos o quedaron bloqueadas y la compuerta no pudo pasar. Su código puede estar bien, pero nadie lo verificó.',
}))

if (escalated.size) log(`Tareas escaladas al usuario: ${[...escalated.keys()].join(', ')}`)
return {
  wave: input.wave,
  verified: [...verified],
  escalated: [...escalated.values()].map(e => ({ ...e, attempts: attempts.get(e.taskId), contextExhausted: exhausted.has(e.taskId) })),
  unaccounted: allTaskIds.filter(id => !verified.has(id) && !escalated.has(id)),
  handoff: { exports: waveExports, decisions: waveDecisions },
  implemented: implemented.map((r, i) => ({ agent: agents[i].id, status: r?.status ?? 'dead', notes: cap(r?.notes, NOTES_LIMIT), files: r?.files ?? [] })),
  history,
}
