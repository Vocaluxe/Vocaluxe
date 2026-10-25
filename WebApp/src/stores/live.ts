// Live data from Vocaluxe (game state, queue, notifications), pushed by the server as Server-Sent Events.
// The browser reconnects by itself when the connection drops.
import { reactive } from 'vue'
import { api, ApiError, eventsUrl, type Notification, type QueueEntry, type State } from '@/api/client'
import { i18n } from '@/i18n'
import { session } from './session'
import { toast } from './toast'

export const live = reactive({
  connected: false,
  /** True once the first connection attempt finished, so the UI does not flash "offline" on start. */
  started: false,
  state: null as State | null,
  queue: null as QueueEntry[] | null
})

let source: EventSource | null = null
let retryTimer: number | undefined

function parse<T>(event: Event): T {
  return JSON.parse((event as MessageEvent<string>).data) as T
}

function onNotification(notification: Notification) {
  if (notification.profile.id === session.profile.value?.id) return
  const t = i18n.global.t
  if (notification.type === 'queueAddedMany') toast(t('queue.notifyAddedMany', { name: notification.profile.name, count: notification.count }))
  else if (notification.song) toast(t('queue.notifyAdded', { name: notification.profile.name, title: notification.song.title }))
}

export function connectLive() {
  if (source) return
  window.clearTimeout(retryTimer)
  source = new EventSource(eventsUrl)
  source.addEventListener('open', () => {
    live.connected = true
    live.started = true
  })
  source.addEventListener('state', (e) => (live.state = parse<State>(e)))
  source.addEventListener('queue', (e) => (live.queue = parse<QueueEntry[]>(e)))
  source.addEventListener('notification', (e) => onNotification(parse<Notification>(e)))
  source.addEventListener('error', () => {
    live.connected = false
    live.started = true
    if (source?.readyState !== EventSource.CLOSED) return
    // The server refused the connection (e.g. logged out) or is gone: check the session, then try again.
    source = null
    api
      .session()
      .then(() => (retryTimer = window.setTimeout(connectLive, 3000)))
      .catch((e) => {
        if (e instanceof ApiError && e.status === 401) session.expire()
        else retryTimer = window.setTimeout(connectLive, 3000)
      })
  })
}

export function disconnectLive() {
  window.clearTimeout(retryTimer)
  source?.close()
  source = null
  live.connected = false
}

/** Use the queue returned by an API call right away, without waiting for the event. */
export function setQueue(queue: QueueEntry[]) {
  live.queue = queue
}
