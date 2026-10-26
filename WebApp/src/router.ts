import { watch } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import AppShell from '@/layouts/AppShell.vue'
import { session } from '@/stores/session'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: () => import('@/views/LoginView.vue'), meta: { public: true } },
    {
      path: '/',
      component: AppShell,
      children: [
        { path: '', redirect: '/now' },
        { path: 'now', name: 'now', component: () => import('@/views/NowView.vue') },
        { path: 'songs', name: 'songs', component: () => import('@/views/SongsView.vue') },
        { path: 'queue', name: 'queue', component: () => import('@/views/QueueView.vue') },
        { path: 'players', name: 'players', component: () => import('@/views/PlayersView.vue') },
        { path: 'more', name: 'more', component: () => import('@/views/MoreView.vue') },
        { path: 'playlists', name: 'playlists', component: () => import('@/views/PlaylistsView.vue') },
        { path: 'playlists/:id(\\d+)', name: 'playlist', component: () => import('@/views/PlaylistView.vue'), props: (r) => ({ id: Number(r.params.id) }) },
        { path: 'help', name: 'help', component: () => import('@/views/HelpView.vue') },
        { path: 'remote', name: 'remote', component: () => import('@/views/RemoteView.vue') },
        { path: 'admin', name: 'admin', component: () => import('@/views/AdminView.vue'), meta: { admin: true } }
      ]
    },
    { path: '/:pathMatch(.*)*', redirect: '/now' }
  ]
})

let restored: Promise<boolean> | null = null

router.beforeEach(async (to) => {
  restored ??= session.restore().catch(() => false)
  const loggedIn = (await restored) && session.state.session !== null
  if (to.meta.public) return loggedIn && to.name === 'login' ? { name: 'now' } : true
  if (!session.state.session) return { name: 'login', query: to.fullPath !== '/now' ? { next: to.fullPath } : {} }
  if (to.meta.admin && !session.isAdmin.value) return { name: 'more' }
  return true
})

// The session can end at any time (role changed, Vocaluxe restarted): go back to the login page.
watch(
  () => session.state.session,
  (value, old) => {
    if (old && !value) {
      restored = Promise.resolve(false)
      router.replace({ name: 'login' })
    }
  }
)
