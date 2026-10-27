<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import AppIcon, { type IconName } from '@/components/AppIcon.vue'
import NowPanel from '@/components/NowPanel.vue'
import ProfileAvatar from '@/components/ProfileAvatar.vue'
import SongCover from '@/components/SongCover.vue'
import { connectLive, disconnectLive, live } from '@/stores/live'
import { session } from '@/stores/session'

const { t, te } = useI18n()
const route = useRoute()
const state = computed(() => live.state)

interface Tab {
  to: string
  label: string
  short?: string
  icon: IconName
  matches: string[]
  visible?: () => boolean
}

const allTabs: Tab[] = [
  { to: '/now', label: 'nav.now', icon: 'mic', matches: ['now'] },
  { to: '/songs', label: 'nav.songs', icon: 'music', matches: ['songs'] },
  { to: '/queue', label: 'nav.queue', short: 'nav.queueShort', icon: 'queue', matches: ['queue'] },
  { to: '/remote', label: 'nav.remote', short: 'nav.remoteShort', icon: 'keyboard', matches: ['remote'], visible: () => session.can('UseKeyboard') },
  { to: '/players', label: 'nav.players', icon: 'users', matches: ['players'] },
  { to: '/more', label: 'nav.more', icon: 'more', matches: ['more', 'playlists', 'playlist', 'admin', 'help'] }
]

const tabs = computed(() => allTabs.filter((tab) => !tab.visible || tab.visible()))
const activeTab = computed(() => tabs.value.find((tab) => tab.matches.includes(String(route.name))))

// Show the connection warning only if the connection stays away for a moment.
const offline = ref(false)
let offlineTimer: number | undefined
watch(
  () => live.started && !live.connected,
  (lost) => {
    window.clearTimeout(offlineTimer)
    if (lost) offlineTimer = window.setTimeout(() => (offline.value = true), 2000)
    else offline.value = false
  }
)

onMounted(connectLive)
onUnmounted(() => {
  window.clearTimeout(offlineTimer)
  disconnectLive()
})
const screenName = computed(() => {
  const screen = state.value?.screen
  return screen && te(`screens.${screen}`) ? t(`screens.${screen}`) : (screen ?? '')
})
</script>

<template>
  <div class="shell">
    <nav class="sidebar" :aria-label="t('nav.more')">
      <div class="brand">
        <img src="/icon.png" alt="" width="32" height="32" />
        <span>Vocaluxe</span>
      </div>
      <RouterLink v-for="tab in tabs" :key="tab.to" :to="tab.to" class="side-link" :class="{ active: activeTab === tab }">
        <AppIcon :name="tab.icon" /> {{ t(tab.label) }}
      </RouterLink>
      <RouterLink v-if="session.profile.value" to="/players" class="me">
        <ProfileAvatar :profile="session.profile.value" :size="32" />
        <span class="row-title">{{ session.profile.value.name }}</span>
      </RouterLink>
    </nav>

    <div v-if="offline" class="offline" role="status">{{ t('live.reconnecting') }}</div>

    <main class="content">
      <RouterView v-slot="{ Component }">
        <component :is="Component" />
      </RouterView>
    </main>

    <aside class="panel">
      <NowPanel :up-next-count="8" />
    </aside>

    <RouterLink v-if="route.name !== 'now'" to="/now" class="nowbar">
      <SongCover v-if="state?.currentSong" :song-id="state.currentSong.id" :has-cover="state.currentSong.hasCover" :size="40" />
      <AppIcon v-else name="mic" />
      <span class="row-text">
        <span class="row-title">{{ state?.currentSong?.title ?? screenName }}</span>
        <span class="row-sub">{{ state?.currentSong ? `${state.currentSong.artist} · ${screenName}` : t('now.noSong') }}</span>
      </span>
    </RouterLink>

    <nav class="tabbar" :aria-label="t('nav.more')">
      <RouterLink v-for="tab in tabs" :key="tab.to" :to="tab.to" class="tab" :class="{ active: activeTab === tab }">
        <AppIcon :name="tab.icon" />
        <span class="tab-label">{{ t(tab.short ?? tab.label) }}</span>
      </RouterLink>
    </nav>
  </div>
</template>

<style scoped>
.shell {
  min-height: 100dvh;
  padding-top: env(safe-area-inset-top);
  padding-bottom: calc(var(--tabbar-height) + var(--nowbar-height) + env(safe-area-inset-bottom));
}

.sidebar,
.panel {
  display: none;
}

.content {
  min-width: 0;
}

/* Phone: tab bar at the bottom, now-playing bar above it */
.tabbar {
  position: fixed;
  inset: auto 0 0 0;
  z-index: 20;
  display: grid;
  grid-auto-columns: 1fr;
  grid-auto-flow: column;
  height: calc(var(--tabbar-height) + env(safe-area-inset-bottom));
  padding-bottom: env(safe-area-inset-bottom);
  background: var(--surface);
  border-top: 1px solid var(--border);
}

.tab {
  display: grid;
  place-items: center;
  align-content: center;
  gap: 2px;
  color: var(--text-muted);
  text-decoration: none;
  font-size: 0.7rem;
  font-weight: 600;
}

.tab.active {
  color: var(--accent);
}

.tab-label {
  max-width: 100%;
  padding: 0 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.65rem;
}

.offline {
  position: sticky;
  top: 0;
  z-index: 30;
  padding: 8px 16px;
  background: var(--error);
  color: #fff;
  text-align: center;
  font-size: 0.875rem;
  font-weight: 600;
}

@media (min-width: 768px) {
  .offline {
    grid-column: 2 / -1;
  }
}

.nowbar {
  position: fixed;
  left: 8px;
  right: 8px;
  bottom: calc(var(--tabbar-height) + env(safe-area-inset-bottom) + 6px);
  z-index: 19;
  display: flex;
  align-items: center;
  gap: 12px;
  height: calc(var(--nowbar-height) - 6px);
  padding: 0 14px 0 6px;
  border-radius: var(--radius);
  background: var(--surface-raised);
  box-shadow: var(--shadow);
  color: inherit;
  text-decoration: none;
}

/* Tablet: navigation on the side */
@media (min-width: 768px) {
  .shell {
    display: grid;
    grid-template-columns: var(--sidebar-width) minmax(0, 1fr);
    padding-bottom: calc(var(--nowbar-height) + 12px);
  }

  .tabbar {
    display: none;
  }

  .content {
    grid-column: 2;
  }

  .sidebar {
    grid-row: 1 / span 2;
    position: sticky;
    top: 0;
    height: 100dvh;
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 16px 12px;
    background: var(--surface);
    border-right: 1px solid var(--border);
  }

  .brand {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 4px 10px 16px;
    font-weight: 700;
    font-size: 1.15rem;
  }

  .brand img {
    border-radius: 8px;
  }

  .side-link {
    display: flex;
    align-items: center;
    gap: 12px;
    min-height: 44px;
    padding: 0 12px;
    border-radius: var(--radius-small);
    color: var(--text-muted);
    text-decoration: none;
    font-weight: 600;
  }

  .side-link:hover {
    background: var(--surface-hover);
  }

  .side-link.active {
    background: var(--accent-soft);
    color: var(--accent);
  }

  .me {
    margin-top: auto;
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px;
    border-radius: var(--radius-small);
    text-decoration: none;
    min-width: 0;
  }

  .me:hover {
    background: var(--surface-hover);
  }

  .nowbar {
    left: calc(var(--sidebar-width) + 16px);
    right: 16px;
    bottom: 12px;
  }
}

/* Desktop: the current song and the queue stay visible on the right */
@media (min-width: 1200px) {
  .shell {
    grid-template-columns: var(--sidebar-width) minmax(0, 1fr) var(--panel-width);
    padding-bottom: 0;
  }

  .panel {
    display: block;
    position: sticky;
    top: 0;
    height: 100dvh;
    overflow-y: auto;
    padding: var(--gutter);
    border-left: 1px solid var(--border);
    grid-column: 3;
    grid-row: 1 / span 2;
  }

  .nowbar {
    display: none;
  }
}
</style>
