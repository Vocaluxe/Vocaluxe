<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, gameAvatarUrl, type Avatar, type Profile, type Role } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import AppSheet from '@/components/AppSheet.vue'
import ProfileAvatar from '@/components/ProfileAvatar.vue'
import { session } from '@/stores/session'
import { showError, toast } from '@/stores/toast'

const { t } = useI18n()
const me = computed(() => session.profile.value)
const canEdit = computed(() => session.can('EditOwnProfile') || session.can('EditAllProfiles'))
const roles: Role[] = ['Guest', 'Player', 'Admin']
const difficulties = computed(() => [t('players.difficulty0'), t('players.difficulty1'), t('players.difficulty2')])

const name = ref('')
const difficulty = ref(0)
const newPin = ref('')
const busy = ref(false)
const profiles = ref<Profile[]>([])
const fileInput = ref<HTMLInputElement>()
const choosing = ref(false)
const avatars = ref<Avatar[]>([])

watch(
  me,
  (profile) => {
    name.value = profile?.name ?? ''
    difficulty.value = profile?.difficulty ?? 0
  },
  { immediate: true }
)

onMounted(loadProfiles)

async function loadProfiles() {
  profiles.value = await api.profiles().catch((e) => {
    showError(e)
    return []
  })
}

async function save() {
  if (!me.value) return
  if (newPin.value && newPin.value.length < 4) {
    toast(t('players.pinTooShort'), 'error')
    return
  }
  busy.value = true
  try {
    const profile = await api.updateProfile(me.value.id, {
      name: name.value.trim() || undefined,
      difficulty: difficulty.value,
      pin: newPin.value || undefined
    })
    session.updateProfile(profile)
    newPin.value = ''
    toast(t('common.saved'))
    loadProfiles()
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}

async function removePin() {
  if (!me.value) return
  busy.value = true
  try {
    session.updateProfile(await api.updateProfile(me.value.id, { pin: '' }))
    toast(t('common.saved'))
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}

async function chooseAvatar() {
  choosing.value = true
  avatars.value = await api.avatars().catch(() => [])
}

async function selectGameAvatar(avatar: Avatar) {
  if (!me.value) return
  busy.value = true
  try {
    session.updateProfile(await api.selectGameAvatar(me.value.id, avatar.id), true)
    choosing.value = false
    loadProfiles()
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}

async function uploadAvatar(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file || !me.value) return
  choosing.value = false
  busy.value = true
  try {
    session.updateProfile(await api.setAvatar(me.value.id, file), true)
    loadProfiles()
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
    if (fileInput.value) fileInput.value.value = ''
  }
}

async function changeRole(profile: Profile, role: Role) {
  try {
    const updated = await api.setRole(profile.id, role)
    profiles.value = profiles.value.map((p) => (p.id === updated.id ? updated : p))
    toast(t('players.roleChanged'))
  } catch (e) {
    showError(e)
    loadProfiles()
  }
}
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('players.title') }}</h1>
    </header>

    <section v-if="me" class="card mine">
      <h2>{{ t('players.myProfile') }}</h2>
      <div class="avatar-row">
        <ProfileAvatar :profile="me" :size="96" />
        <div v-if="canEdit">
          <input ref="fileInput" type="file" accept="image/*" class="visually-hidden" @change="uploadAvatar" />
          <button class="btn" :disabled="busy" @click="chooseAvatar"><AppIcon name="camera" /> {{ t('players.changeAvatar') }}</button>
        </div>
      </div>

      <form class="form" @submit.prevent="save">
        <label class="field">
          <span>{{ t('players.name') }}</span>
          <input v-model="name" class="input" maxlength="32" :disabled="!canEdit" />
        </label>
        <label class="field">
          <span>{{ t('players.difficulty') }}</span>
          <select v-model.number="difficulty" class="select" :disabled="!canEdit">
            <option v-for="(label, index) in difficulties" :key="index" :value="index">{{ label }}</option>
          </select>
        </label>
        <div class="field">
          <span>{{ t('players.pin') }} · {{ me.hasPin ? t('players.pinSet') : t('players.pinNone') }}</span>
          <input
            v-model="newPin"
            class="input"
            type="password"
            inputmode="numeric"
            autocomplete="new-password"
            maxlength="32"
            :placeholder="t('players.newPin')"
            :disabled="!canEdit"
          />
          <small class="muted">{{ t('players.pinHint') }}</small>
        </div>
        <div v-if="canEdit" class="buttons">
          <button class="btn btn--primary" :disabled="busy">{{ t('common.save') }}</button>
          <button v-if="me.hasPin" type="button" class="btn btn--danger" :disabled="busy" @click="removePin">{{ t('players.removePin') }}</button>
        </div>
      </form>
    </section>

    <section class="all">
      <h2>{{ t('players.allProfiles') }}</h2>
      <ul class="list">
        <li v-for="p in profiles" :key="p.id" class="row">
          <ProfileAvatar :profile="p" :size="40" />
          <span class="row-text">
            <span class="row-title">{{ p.name }} <AppIcon v-if="p.hasPin" name="lock" :size="14" class="muted" /></span>
            <span v-if="!session.isAdmin.value" class="row-sub">{{ t(`players.roles.${p.role}`) }}</span>
          </span>
          <select
            v-if="session.isAdmin.value"
            class="select role"
            :value="p.role"
            :disabled="p.id === me?.id"
            :aria-label="t('players.role')"
            @change="changeRole(p, ($event.target as HTMLSelectElement).value as Role)"
          >
            <option v-for="r in roles" :key="r" :value="r">{{ t(`players.roles.${r}`) }}</option>
          </select>
        </li>
      </ul>
    </section>

    <AppSheet :open="choosing" :title="t('players.chooseAvatar')" @close="choosing = false">
      <button class="btn btn--block" :disabled="busy" @click="fileInput?.click()"><AppIcon name="camera" /> {{ t('players.ownPhoto') }}</button>
      <template v-if="avatars.length">
        <h3 class="avatars-title">{{ t('players.gameAvatars') }}</h3>
        <div class="avatars">
          <button v-for="a in avatars" :key="a.id" type="button" class="avatar-choice" :disabled="busy" @click="selectGameAvatar(a)">
            <img :src="gameAvatarUrl(a.id, 160)" alt="" loading="lazy" />
          </button>
        </div>
      </template>
    </AppSheet>
  </div>
</template>

<style scoped>
.mine,
.all {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 14px;
}

.avatars-title {
  margin: 16px 0 8px;
  font-size: 0.875rem;
  color: var(--text-muted);
}

.avatars {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(72px, 1fr));
  gap: 10px;
}

.avatar-choice {
  aspect-ratio: 1;
  padding: 0;
  border: 2px solid transparent;
  border-radius: 50%;
  overflow: hidden;
  background: #fff;
}

.avatar-choice:hover:not(:disabled),
.avatar-choice:focus-visible {
  border-color: var(--accent);
}

.avatar-choice img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.avatar-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 16px;
}

.form {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 14px;
}

.buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.role {
  width: auto;
  min-height: 40px;
}

@media (min-width: 1000px) {
  .page {
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
    align-items: start;
  }

  .page-header {
    grid-column: 1 / -1;
  }
}
</style>
