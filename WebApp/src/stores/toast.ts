import { reactive } from 'vue'
import { ApiError } from '@/api/client'
import { i18n } from '@/i18n'
import { session } from './session'

export interface Toast {
  id: number
  text: string
  kind: 'info' | 'error'
}

let nextId = 1
export const toasts = reactive<Toast[]>([])

export function toast(text: string, kind: Toast['kind'] = 'info') {
  const entry = { id: nextId++, text, kind }
  toasts.push(entry)
  window.setTimeout(() => {
    const index = toasts.findIndex((t) => t.id === entry.id)
    if (index >= 0) toasts.splice(index, 1)
  }, 3500)
}

/** Shows an API error in a friendly way. Returns nothing so it can be used as `.catch(showError)`. */
export function showError(error: unknown) {
  const t = i18n.global.t
  if (error instanceof ApiError) {
    if (error.status === 401) {
      session.expire()
      return
    }
    if (error.status === 0) return toast(t('errors.offline'), 'error')
    if (error.status === 403) return toast(t('errors.forbidden'), 'error')
    if (error.status === 503) return toast(t('errors.busy'), 'error')
    return toast(error.message, 'error')
  }
  toast(String(error), 'error')
}
