<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, ApiError, type QueueEntry } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import AppSheet from '@/components/AppSheet.vue'
import ProfileAvatar from '@/components/ProfileAvatar.vue'
import QueueOptions, { type QueueChoice } from '@/components/QueueOptions.vue'
import SongCover from '@/components/SongCover.vue'
import { live, setQueue } from '@/stores/live'
import { session } from '@/stores/session'
import { showError, toast } from '@/stores/toast'

const { t } = useI18n()
const entries = computed(() => live.queue)
const busy = ref(false)
const editing = ref<QueueEntry | null>(null)
const choice = ref<QueueChoice>({ gameMode: 'Normal', audioMode: 'Normal', playerIds: [] })

const isMine = (entry: QueueEntry) => entry.requestedBy?.id === session.profile.value?.id
const canEdit = (entry: QueueEntry) => isMine(entry) || session.can('ManageQueue')
const canOpen = (entry: QueueEntry) => canEdit(entry) || session.can('StartSongs')

function edit(entry: QueueEntry) {
  if (!canOpen(entry)) return
  editing.value = entry
  choice.value = { gameMode: entry.gameMode, audioMode: entry.audioMode, playerIds: entry.players.map((p) => p.id) }
}

async function run(action: () => Promise<QueueEntry[]>, message?: string) {
  busy.value = true
  try {
    setQueue(await action())
    if (message) toast(message)
    return true
  } catch (e) {
    showError(e)
    return false
  } finally {
    busy.value = false
  }
}

async function save() {
  const entry = editing.value
  if (entry && (await run(() => api.updateQueueEntry(entry.id, choice.value), t('common.saved')))) editing.value = null
}

async function remove() {
  const entry = editing.value
  if (entry && (await run(() => api.dequeue(entry.id), t('queue.removed')))) editing.value = null
}

async function start() {
  const entry = editing.value
  if (!entry) return
  busy.value = true
  try {
    setQueue(await api.startQueueEntry(entry.id))
    toast(t('queue.started', { title: entry.song.title }))
    editing.value = null
  } catch (e) {
    if (e instanceof ApiError && e.status === 409) toast(t('queue.cannotStart'), 'error')
    else showError(e)
  } finally {
    busy.value = false
  }
}

const move = (entry: QueueEntry, position: number) => run(() => api.moveInQueue(entry.id, position))

async function clear() {
  if (window.confirm(t('queue.clearConfirm'))) await run(() => api.clearQueue(), t('queue.cleared'))
}
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('queue.title') }}</h1>
      <div class="header-actions">
        <RouterLink to="/help" class="icon-btn" :aria-label="t('more.help')"><AppIcon name="help" /></RouterLink>
        <button v-if="session.isAdmin.value && entries?.length" class="btn btn--danger" :disabled="busy" @click="clear">
          <AppIcon name="trash" :size="18" /> <span class="hide-small">{{ t('queue.clear') }}</span>
        </button>
        <RouterLink v-if="session.can('AddToQueue')" to="/songs" class="btn btn--primary" :aria-label="t('nav.songs')"><AppIcon name="plus" /> <span class="hide-small">{{ t('nav.songs') }}</span></RouterLink>
      </div>
    </header>
    <p class="muted hint">{{ t('queue.fairHint') }}</p>

    <p v-if="entries === null" class="muted">{{ t('common.loading') }}</p>
    <div v-else-if="entries.length === 0" class="empty">
      <AppIcon name="queue" :size="40" />
      <p>{{ t('queue.empty') }}</p>
      <p>{{ t('queue.emptyHint') }}</p>
    </div>
    <ol v-else class="list">
      <li v-for="(entry, index) in entries" :key="entry.id" class="entry" :class="{ mine: isMine(entry) }">
        <button class="row" :disabled="!canOpen(entry)" @click="edit(entry)">
          <span class="pos">{{ index + 1 }}</span>
          <SongCover :song-id="entry.song.id" :has-cover="entry.song.hasCover" />
          <span class="row-text">
            <span class="row-title">{{ entry.song.title }}</span>
            <span class="row-sub">{{ entry.song.artist }}</span>
            <span class="meta">
              <span v-if="entry.requestedBy" class="who">
                <ProfileAvatar :profile="{ id: entry.requestedBy.id, name: entry.requestedBy.name, hasAvatar: true }" :size="22" />
                {{ entry.requestedBy.name }}
                <span v-if="isMine(entry)" class="badge badge--accent">{{ t('queue.you') }}</span>
              </span>
              <span v-else class="muted">{{ t('queue.unknown') }}</span>
              <span v-if="entry.gameMode !== 'Normal'" class="badge">{{ t(`gameModes.${entry.gameMode}`) }}</span>
              <span v-if="entry.audioMode !== 'Normal'" class="badge">{{ t(`audioModes.${entry.audioMode}`) }}</span>
            </span>
            <span v-if="entry.players.length" class="row-sub">{{ t('queue.singers', { names: entry.players.map((p) => p.name).join(', ') }) }}</span>
          </span>
          <AppIcon v-if="canOpen(entry)" name="edit" :size="18" class="muted" />
        </button>
        <div v-if="session.can('ManageQueue')" class="move">
          <button class="icon-btn" :disabled="busy || index === 0" :aria-label="t('queue.moveUp')" @click="move(entry, index - 1)">
            <AppIcon name="chevronUp" />
          </button>
          <button class="icon-btn" :disabled="busy || index === entries.length - 1" :aria-label="t('queue.moveDown')" @click="move(entry, index + 1)">
            <AppIcon name="chevronDown" />
          </button>
        </div>
      </li>
    </ol>

    <AppSheet :open="editing !== null" :title="t('queue.edit')" @close="editing = null">
      <template v-if="editing">
        <div class="edit-song">
          <SongCover :song-id="editing.song.id" :has-cover="editing.song.hasCover" :size="56" />
          <span class="row-text">
            <span class="row-title">{{ editing.song.title }}</span>
            <span class="row-sub">{{ editing.song.artist }}</span>
          </span>
        </div>
        <QueueOptions v-if="canEdit(editing)" v-model="choice" :song="editing.song" />
        <button v-if="session.can('StartSongs')" class="btn btn--primary btn--block" :disabled="busy" @click="start">
          <AppIcon name="mic" :size="18" /> {{ t('queue.startNow') }}
        </button>
        <div v-if="canEdit(editing)" class="edit-actions">
          <button class="btn btn--danger" :disabled="busy" @click="remove"><AppIcon name="trash" :size="18" /> {{ t('queue.remove') }}</button>
          <button class="btn" :disabled="busy" @click="save">{{ t('common.save') }}</button>
        </div>
      </template>
    </AppSheet>
  </div>
</template>

<style scoped>
.page-header h1 {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.header-actions {
  display: flex;
  flex: none;
  gap: 8px;
}

.hint {
  margin: -8px 0 0;
  font-size: 0.875rem;
}

.entry {
  display: flex;
  align-items: center;
  border-bottom: 1px solid var(--border);
}

.entry:last-child {
  border-bottom: 0;
}

.entry .row {
  flex: 1;
  min-width: 0;
  border-bottom: 0;
}

.entry .row:disabled {
  opacity: 1;
  cursor: default;
}

.mine {
  background: var(--accent-soft);
}

.pos {
  width: 20px;
  flex: none;
  text-align: center;
  color: var(--text-muted);
  font-variant-numeric: tabular-nums;
}

.meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  margin-top: 4px;
  font-size: 0.8rem;
}

.who {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-weight: 600;
}

.move {
  display: flex;
  flex-direction: column;
  padding-right: 4px;
}

.move .icon-btn {
  height: 32px;
}

.edit-song {
  display: flex;
  align-items: center;
  gap: 12px;
}

.edit-actions {
  display: flex;
  justify-content: space-between;
  gap: 10px;
}

@media (max-width: 480px) {
  .hide-small {
    display: none;
  }

  .header-actions .btn {
    padding: 0 12px;
  }
}
</style>
