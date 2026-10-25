<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { live } from '@/stores/live'
import { session } from '@/stores/session'
import SongCover from './SongCover.vue'

const props = defineProps<{ upNextCount?: number }>()
const { t, te } = useI18n()
const state = computed(() => live.state)
const entries = computed(() => live.queue)

const screenName = computed(() => {
  const screen = state.value?.screen
  if (!screen) return '–'
  return te(`screens.${screen}`) ? t(`screens.${screen}`) : screen
})
const upNext = computed(() => (entries.value ?? []).slice(0, props.upNextCount ?? 5))
</script>

<template>
  <section class="now">
    <div class="current card">
      <SongCover v-if="state?.currentSong" :song-id="state.currentSong.id" :has-cover="state.currentSong.hasCover" :size="72" />
      <div class="current-text">
        <span class="muted small">{{ screenName }}</span>
        <template v-if="state?.currentSong">
          <strong class="ellipsis">{{ state.currentSong.title }}</strong>
          <span class="muted ellipsis">{{ state.currentSong.artist }}</span>
        </template>
        <span v-else class="muted">{{ t('now.noSong') }}</span>
      </div>
    </div>

    <div class="up-next">
      <h2>{{ t('now.upNext') }}</h2>
      <p v-if="upNext.length === 0" class="muted">{{ t('queue.empty') }}</p>
      <ol v-else class="list">
        <li v-for="entry in upNext" :key="entry.id" class="row">
          <SongCover :song-id="entry.song.id" :has-cover="entry.song.hasCover" :size="40" />
          <span class="row-text">
            <span class="row-title">{{ entry.song.title }}</span>
            <span class="row-sub">
              {{ entry.song.artist }}
              <template v-if="entry.requestedBy">
                · {{ entry.requestedBy.name }}<template v-if="entry.requestedBy.id === session.profile.value?.id"> ({{ t('queue.you') }})</template>
              </template>
            </span>
          </span>
        </li>
      </ol>
    </div>
  </section>
</template>

<style scoped>
.now {
  display: grid;
  gap: var(--gutter);
  align-content: start;
}

.current {
  display: flex;
  align-items: center;
  gap: 14px;
}

.current-text {
  display: grid;
  min-width: 0;
}

.small {
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.ellipsis {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.up-next {
  display: grid;
  gap: 10px;
}

.up-next .row {
  min-height: 56px;
}

.up-next p {
  margin: 0;
}
</style>
