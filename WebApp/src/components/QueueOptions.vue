<script setup lang="ts">
// Game mode, audio mode and singers of a wish. Only shows what the song supports.
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, maxPlayers, type AudioMode, type GameMode, type Profile, type Song } from '@/api/client'
import ProfileAvatar from './ProfileAvatar.vue'

export interface QueueChoice {
  gameMode: GameMode
  audioMode: AudioMode
  playerIds: string[]
}

const props = defineProps<{ song: Song }>()
const choice = defineModel<QueueChoice>({ required: true })
const { t } = useI18n()

let profileCache: Promise<Profile[]> | null = null
const profiles = ref<Profile[]>([])

onMounted(async () => {
  profileCache ??= api.profiles().catch(() => [])
  profiles.value = await profileCache
})

const gameModes = computed(() => props.song.gameModes)
const audioModes = computed(() => props.song.audioModes)

function togglePlayer(id: string) {
  const ids = choice.value.playerIds
  if (ids.includes(id)) choice.value = { ...choice.value, playerIds: ids.filter((p) => p !== id) }
  else if (ids.length < maxPlayers) choice.value = { ...choice.value, playerIds: [...ids, id] }
}
</script>

<template>
  <div class="options">
    <fieldset v-if="gameModes.length > 1" class="group">
      <legend>{{ t('songs.gameMode') }}</legend>
      <div class="segments">
        <label v-for="mode in gameModes" :key="mode" class="segment" :class="{ active: choice.gameMode === mode }">
          <input v-model="choice.gameMode" type="radio" :value="mode" class="visually-hidden" />
          {{ t(`gameModes.${mode}`) }}
        </label>
      </div>
    </fieldset>

    <fieldset v-if="audioModes.length > 1" class="group">
      <legend>{{ t('songs.audioMode') }}</legend>
      <div class="segments">
        <label v-for="mode in audioModes" :key="mode" class="segment" :class="{ active: choice.audioMode === mode }">
          <input v-model="choice.audioMode" type="radio" :value="mode" class="visually-hidden" />
          {{ t(`audioModes.${mode}`) }}
        </label>
      </div>
    </fieldset>

    <fieldset class="group">
      <legend>{{ t('songs.players') }}</legend>
      <p class="hint muted">{{ t('songs.playersHint') }}</p>
      <div class="players">
        <button
          v-for="p in profiles"
          :key="p.id"
          type="button"
          class="player"
          :class="{ active: choice.playerIds.includes(p.id) }"
          :aria-pressed="choice.playerIds.includes(p.id)"
          :disabled="!choice.playerIds.includes(p.id) && choice.playerIds.length >= maxPlayers"
          @click="togglePlayer(p.id)"
        >
          <ProfileAvatar :profile="p" :size="28" />
          <span>{{ p.name }}</span>
          <span v-if="choice.playerIds.includes(p.id)" class="order">{{ choice.playerIds.indexOf(p.id) + 1 }}</span>
        </button>
      </div>
    </fieldset>
  </div>
</template>

<style scoped>
.options {
  display: grid;
  gap: 16px;
}

.group {
  display: grid;
  gap: 8px;
  margin: 0;
  padding: 0;
  border: 0;
  min-width: 0;
}

.group legend {
  padding: 0;
  margin-bottom: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-muted);
}

.hint {
  margin: -6px 0 0;
  font-size: 0.8rem;
}

.segments {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.segment {
  position: relative;
  display: inline-flex;
  align-items: center;
  min-height: 40px;
  padding: 0 14px;
  border: 1px solid var(--border);
  border-radius: 999px;
  cursor: pointer;
  font-weight: 600;
}

.segment.active {
  border-color: var(--accent);
  background: var(--accent-soft);
  color: var(--accent);
}

.players {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.player {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 40px;
  padding: 4px 12px 4px 4px;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: transparent;
}

.player.active {
  border-color: var(--accent);
  background: var(--accent-soft);
}

.order {
  display: grid;
  place-items: center;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: var(--accent);
  color: #fff;
  font-size: 0.75rem;
  font-weight: 700;
}
</style>
