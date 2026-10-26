<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, type AdminSession, type AdminSettings, type AdminStatus, type Permission, type Profile } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import ProfileAvatar from '@/components/ProfileAvatar.vue'
import { session } from '@/stores/session'
import { showError, toast } from '@/stores/toast'

const { t, locale } = useI18n()
const permissions: Permission[] = ['AddToQueue', 'ManageQueue', 'UseKeyboard', 'EditPlaylists', 'DeletePlaylists', 'EditOwnProfile', 'EditAllProfiles', 'CreateProfiles', 'StartSongs']
const settings = ref<AdminSettings | null>(null)
const status = ref<AdminStatus | null>(null)
const sessions = ref<AdminSession[]>([])
const profiles = ref<Profile[]>([])
const busy = ref(false)
const me = computed(() => session.profile.value)
const time = (value: string) => new Date(value).toLocaleTimeString(locale.value, { hour: '2-digit', minute: '2-digit' })

onMounted(async () => {
  settings.value = await api.adminSettings().catch((e) => {
    showError(e)
    return null
  })
  await Promise.all([loadStatus(), loadSessions(), loadProfiles()])
})

async function loadStatus() {
  status.value = await api.adminStatus().catch(() => null)
}

async function loadSessions() {
  sessions.value = await api.adminSessions().catch(() => [])
}

async function loadProfiles() {
  profiles.value = await api.adminProfiles().catch(() => [])
}

function toggle(list: Permission[], permission: Permission, on: boolean) {
  const index = list.indexOf(permission)
  if (on && index < 0) list.push(permission)
  if (!on && index >= 0) list.splice(index, 1)
}

async function save() {
  if (!settings.value) return
  busy.value = true
  try {
    settings.value = await api.saveAdminSettings(settings.value)
    toast(t('common.saved'))
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}

async function logoutDevice(s: AdminSession) {
  try {
    await api.deleteSession(s.id)
    toast(t('admin.deviceLoggedOut', { name: s.profile.name }))
  } catch (e) {
    showError(e)
  }
  await loadSessions()
}

async function setActive(p: Profile, active: boolean) {
  try {
    await api.setProfileActive(p.id, active)
  } catch (e) {
    showError(e)
  }
  await Promise.all([loadProfiles(), loadSessions()])
}

async function deleteProfile(p: Profile) {
  if (!window.confirm(t('admin.deleteProfileConfirm', { name: p.name }))) return
  try {
    await api.deleteProfile(p.id)
    toast(t('admin.profileDeleted', { name: p.name }))
  } catch (e) {
    showError(e)
  }
  await Promise.all([loadProfiles(), loadSessions()])
}
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('admin.title') }}</h1>
    </header>

    <section v-if="status" class="card">
      <h2>{{ t('admin.server') }}</h2>
      <dl class="facts">
        <dt>{{ t('admin.address') }}</dt>
        <dd>{{ status.address.replace(/\/$/, '') }}</dd>
        <dt>{{ t('admin.port') }}</dt>
        <dd>{{ status.port }}</dd>
        <dt>HTTPS</dt>
        <dd>
          <span :class="['badge', { 'badge--accent': status.https }]">{{ status.https ? t('admin.on') : t('admin.off') }}</span>
        </dd>
        <dt>{{ t('admin.connected') }}</dt>
        <dd>{{ status.connectedClients }}</dd>
        <dt>{{ t('admin.version') }}</dt>
        <dd>{{ status.version }}</dd>
      </dl>
      <p class="muted">{{ t('admin.serverHint') }}</p>
    </section>

    <p v-if="!settings" class="muted">{{ t('common.loading') }}</p>
    <form v-else class="form" @submit.prevent="save">
      <section class="card">
        <h2>{{ t('admin.general') }}</h2>
        <label class="check">
          <input v-model="settings.allowProfileCreation" type="checkbox" />
          <span>{{ t('admin.allowProfileCreation') }}</span>
        </label>
        <label class="field">
          <span>{{ t('admin.queuePlaylistName') }}</span>
          <input v-model="settings.queuePlaylistName" class="input" maxlength="64" required />
        </label>
      </section>

      <section class="card">
        <h2>{{ t('admin.permissions') }}</h2>
        <p class="muted">{{ t('admin.permissionsHint') }}</p>
        <table class="matrix">
          <colgroup>
            <col class="perm" />
            <col class="role" />
            <col class="role" />
            <col class="role" />
          </colgroup>
          <thead>
            <tr>
              <th></th>
              <th>{{ t('players.roles.Guest') }}</th>
              <th>{{ t('players.roles.Player') }}</th>
              <th>{{ t('players.roles.Admin') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in permissions" :key="p">
              <th scope="row">{{ t(`admin.perms.${p}`) }}</th>
              <td>
                <input
                  type="checkbox"
                  :checked="settings.guestPermissions.includes(p)"
                  :aria-label="`${t('players.roles.Guest')}: ${t(`admin.perms.${p}`)}`"
                  @change="toggle(settings.guestPermissions, p, ($event.target as HTMLInputElement).checked)"
                />
              </td>
              <td>
                <input
                  type="checkbox"
                  :checked="settings.playerPermissions.includes(p)"
                  :aria-label="`${t('players.roles.Player')}: ${t(`admin.perms.${p}`)}`"
                  @change="toggle(settings.playerPermissions, p, ($event.target as HTMLInputElement).checked)"
                />
              </td>
              <td><input type="checkbox" checked disabled :aria-label="`${t('players.roles.Admin')}: ${t(`admin.perms.${p}`)}`" /></td>
            </tr>
          </tbody>
        </table>
      </section>

      <button class="btn btn--primary" :disabled="busy">{{ t('common.save') }}</button>
    </form>

    <section class="group">
      <h2>{{ t('admin.devices') }}</h2>
      <p class="muted">{{ t('admin.devicesHint') }}</p>
      <ul class="list">
        <li v-for="s in sessions" :key="s.id" class="row">
          <span class="row-text">
            <span class="row-title">
              {{ s.profile.name }} <span v-if="s.isCurrent" class="badge badge--accent">{{ t('admin.thisDevice') }}</span>
            </span>
            <span class="row-sub">{{ [s.device, s.remoteAddress, time(s.lastSeen)].filter(Boolean).join(' · ') }}</span>
          </span>
          <button v-if="!s.isCurrent" type="button" class="icon-btn" :aria-label="t('admin.logoutDevice')" :title="t('admin.logoutDevice')" @click="logoutDevice(s)">
            <AppIcon name="logout" :size="20" />
          </button>
        </li>
      </ul>
    </section>

    <section class="group">
      <h2>{{ t('admin.profiles') }}</h2>
      <p class="muted">{{ t('admin.profilesHint') }}</p>
      <ul class="list">
        <li v-for="p in profiles" :key="p.id" :class="['row', { inactive: !p.isActive }]">
          <ProfileAvatar :profile="p" :size="40" />
          <span class="row-text">
            <span class="row-title">{{ p.name }}</span>
            <span class="row-sub">{{ t(`players.roles.${p.role}`) }}{{ p.isActive ? '' : ' · ' + t('admin.inactive') }}</span>
          </span>
          <label class="switch" :title="t('admin.active')">
            <input
              type="checkbox"
              :checked="p.isActive"
              :disabled="p.id === me?.id"
              :aria-label="`${t('admin.active')}: ${p.name}`"
              @change="setActive(p, ($event.target as HTMLInputElement).checked)"
            />
          </label>
          <button
            type="button"
            class="icon-btn btn--danger"
            :disabled="p.id === me?.id"
            :aria-label="`${t('admin.deleteProfile')}: ${p.name}`"
            :title="t('admin.deleteProfile')"
            @click="deleteProfile(p)"
          >
            <AppIcon name="trash" :size="20" />
          </button>
        </li>
      </ul>
    </section>
  </div>
</template>

<style scoped>
.form,
.card,
.group {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 12px;
}

.group h2,
.group p {
  margin: 0;
}

.facts {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 6px 16px;
  margin: 0;
}

.facts dt {
  color: var(--text-muted);
}

.facts dd {
  margin: 0;
  overflow-wrap: anywhere;
}

.inactive .row-title {
  color: var(--text-muted);
}

.switch input {
  width: 22px;
  height: 22px;
  accent-color: var(--accent);
}

.icon-btn:disabled {
  opacity: 0.35;
}

.card p {
  margin: 0;
}

.matrix {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
}

.matrix col.perm {
  width: auto;
}

.matrix col.role {
  width: 18%;
}

.matrix th,
.matrix td {
  padding: 10px 6px;
  border-bottom: 1px solid var(--border);
  text-align: center;
}

.matrix tbody th {
  text-align: left;
  font-weight: 400;
  overflow-wrap: anywhere;
  hyphens: auto;
}

.matrix thead th {
  padding: 10px 4px;
  font-size: 0.8125rem;
  color: var(--text-muted);
  overflow-wrap: anywhere;
  hyphens: auto;
}

.matrix input {
  width: 22px;
  height: 22px;
  accent-color: var(--accent);
}
</style>
