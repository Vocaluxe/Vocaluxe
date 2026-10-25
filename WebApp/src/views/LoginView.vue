<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { api, ApiError, type Profile } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import AppSheet from '@/components/AppSheet.vue'
import ProfileAvatar from '@/components/ProfileAvatar.vue'
import { session } from '@/stores/session'
import { showError } from '@/stores/toast'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const profiles = ref<Profile[] | null>(null)
const unreachable = ref(false)
const pinFor = ref<Profile | null>(null)
const pin = ref('')
const pinError = ref('')
const creating = ref(false)
const newName = ref('')
const newPin = ref('')
const busy = ref(false)

const server = computed(() => session.state.server)

async function load() {
  unreachable.value = false
  try {
    session.state.server = await api.server()
    profiles.value = await api.profiles()
  } catch {
    unreachable.value = true
  }
}

onMounted(load)

function done() {
  const next = typeof route.query.next === 'string' && route.query.next.startsWith('/') ? route.query.next : '/now'
  router.replace(next)
}

async function pick(profile: Profile) {
  if (profile.hasPin) {
    pinFor.value = profile
    pin.value = ''
    pinError.value = ''
    return
  }
  await login(profile)
}

async function login(profile: Profile, withPin?: string) {
  busy.value = true
  try {
    await session.login(profile, withPin)
    pinFor.value = null
    done()
  } catch (e) {
    if (e instanceof ApiError && (e.status === 403 || e.status === 429) && pinFor.value) {
      pinError.value = e.status === 429 ? t('login.locked') : t('login.wrongPin')
      pin.value = ''
    } else {
      showError(e)
    }
  } finally {
    busy.value = false
  }
}

async function create() {
  if (!newName.value.trim()) return
  busy.value = true
  try {
    const pinValue = newPin.value || undefined
    const profile = await api.createProfile(newName.value.trim(), pinValue)
    await session.login(profile, pinValue)
    creating.value = false
    done()
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <main class="login">
    <header class="login-header">
      <img src="/icon.png" alt="" width="56" height="56" />
      <h1>{{ t('login.title') }}</h1>
      <p class="muted">{{ t('login.subtitle') }}</p>
    </header>

    <div v-if="unreachable" class="card notice">
      <p>{{ t('errors.startup') }}</p>
      <button class="btn btn--primary" @click="load">{{ t('common.retry') }}</button>
    </div>

    <p v-else-if="profiles === null" class="muted center">{{ t('common.loading') }}</p>

    <template v-else>
      <p v-if="profiles.length === 0 && !server?.allowProfileCreation" class="muted center">{{ t('login.noProfiles') }}</p>
      <ul class="tiles">
        <li v-for="p in profiles" :key="p.id">
          <button class="tile" :disabled="busy" @click="pick(p)">
            <ProfileAvatar :profile="p" :size="88" />
            <span class="tile-name">{{ p.name }}</span>
            <AppIcon v-if="p.hasPin" name="lock" :size="16" class="tile-lock" />
          </button>
        </li>
        <li v-if="server?.allowProfileCreation">
          <button class="tile tile--new" :disabled="busy" @click="creating = true">
            <span class="tile-plus"><AppIcon name="plus" :size="36" /></span>
            <span class="tile-name">{{ t('login.newProfile') }}</span>
          </button>
        </li>
      </ul>
    </template>

    <AppSheet :open="pinFor !== null" :title="pinFor ? t('login.pinTitle', { name: pinFor.name }) : ''" @close="pinFor = null">
      <form class="form" @submit.prevent="pinFor && login(pinFor, pin)">
        <label class="field">
          <span>{{ t('login.pin') }}</span>
          <input v-model="pin" class="input" type="password" inputmode="numeric" autocomplete="current-password" autofocus />
        </label>
        <p v-if="pinError" class="error">{{ pinError }}</p>
        <button class="btn btn--primary btn--block" :disabled="busy || !pin">{{ t('login.enter') }}</button>
      </form>
    </AppSheet>

    <AppSheet :open="creating" :title="t('login.newProfile')" @close="creating = false">
      <form class="form" @submit.prevent="create">
        <label class="field">
          <span>{{ t('login.name') }}</span>
          <input v-model="newName" class="input" maxlength="32" autocomplete="nickname" required autofocus />
        </label>
        <label class="field">
          <span>{{ t('login.pinOptional') }}</span>
          <input v-model="newPin" class="input" type="password" inputmode="numeric" minlength="4" maxlength="32" autocomplete="new-password" />
        </label>
        <button class="btn btn--primary btn--block" :disabled="busy || !newName.trim() || (newPin.length > 0 && newPin.length < 4)">
          {{ t('login.create') }}
        </button>
      </form>
    </AppSheet>
  </main>
</template>

<style scoped>
.login {
  display: grid;
  gap: 24px;
  align-content: start;
  max-width: 760px;
  margin: 0 auto;
  padding: calc(32px + env(safe-area-inset-top)) var(--gutter) 32px;
}

.login-header {
  display: grid;
  justify-items: center;
  gap: 8px;
  text-align: center;
}

.login-header img {
  border-radius: 14px;
}

.login-header p {
  margin: 0;
}

.center {
  text-align: center;
}

.notice {
  display: grid;
  justify-items: center;
  gap: 12px;
  text-align: center;
}

.notice p {
  margin: 0;
}

.tiles {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 12px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.tile {
  position: relative;
  display: grid;
  justify-items: center;
  gap: 10px;
  width: 100%;
  padding: 18px 8px 14px;
  border: 0;
  border-radius: var(--radius);
  background: var(--surface);
}

.tile:hover:not(:disabled) {
  background: var(--surface-hover);
}

.tile-name {
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-weight: 600;
}

.tile-lock {
  position: absolute;
  top: 10px;
  right: 10px;
  color: var(--text-muted);
}

.tile-plus {
  display: grid;
  place-items: center;
  width: 88px;
  height: 88px;
  border-radius: 50%;
  border: 2px dashed var(--border);
  color: var(--text-muted);
}

.form {
  display: grid;
  gap: 14px;
}

.error {
  margin: 0;
  color: var(--error);
}
</style>
