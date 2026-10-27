import { computed, reactive } from 'vue'
import { api, ApiError, type Permission, type Profile, type ServerInfo, type Session } from '@/api/client'
import { storage } from './storage'

const lastProfileKey = 'vlx.lastProfile'

const state = reactive({
  server: null as ServerInfo | null,
  session: null as Session | null,
  /** Changes when the own avatar changes, so images reload. */
  avatarVersion: ''
})

export const session = {
  state,
  profile: computed(() => state.session?.profile ?? null),
  isAdmin: computed(() => state.session?.profile.role === 'Admin'),
  can: (permission: Permission) => state.session?.permissions.includes(permission) ?? false,

  /** Restores the session after a reload. After a restart of Vocaluxe profiles without PIN are logged in again silently. */
  async restore(): Promise<boolean> {
    state.server ??= await api.server()
    try {
      state.session = await api.session()
      return true
    } catch (e) {
      if (!(e instanceof ApiError) || e.status !== 401) throw e
    }

    const last = storage.get(lastProfileKey)
    if (!last) return false
    const profile = (await api.profiles()).find((p) => p.id === last)
    if (!profile || profile.hasPin) return false
    try {
      await this.login(profile)
      return true
    } catch {
      return false
    }
  },

  async login(profile: Profile, pin?: string) {
    state.session = await api.login(profile.id, pin)
    storage.set(lastProfileKey, profile.id)
  },

  async logout() {
    await api.logout().catch(() => undefined)
    storage.remove(lastProfileKey)
    state.session = null
  },

  /** Called when the API says 401: the session expired or the role changed. */
  expire() {
    state.session = null
  },

  updateProfile(profile: Profile, avatarChanged = false) {
    if (state.session && state.session.profile.id === profile.id) {
      state.session.profile = profile
      if (avatarChanged) state.avatarVersion = String(Date.now())
    }
  },

  async refresh() {
    state.session = await api.session()
  }
}
