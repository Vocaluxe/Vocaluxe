<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import AppIcon from '@/components/AppIcon.vue'
import ProfileAvatar from '@/components/ProfileAvatar.vue'
import { languages, setLanguage } from '@/i18n'
import { session } from '@/stores/session'

const { t, locale } = useI18n()
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('more.title') }}</h1>
    </header>

    <div v-if="session.profile.value" class="card me">
      <ProfileAvatar :profile="session.profile.value" :size="48" />
      <span class="row-text">
        <span class="row-title">{{ t('more.loggedInAs', { name: session.profile.value.name }) }}</span>
        <span class="row-sub">{{ t(`players.roles.${session.profile.value.role}`) }}</span>
      </span>
    </div>

    <nav class="list">
      <RouterLink to="/playlists" class="row"><AppIcon name="playlist" /><span class="row-text row-title">{{ t('more.playlists') }}</span><AppIcon name="right" class="muted" /></RouterLink>
      <RouterLink v-if="session.isAdmin.value" to="/admin" class="row">
        <AppIcon name="shield" /><span class="row-text row-title">{{ t('more.admin') }}</span><AppIcon name="right" class="muted" />
      </RouterLink>
      <RouterLink to="/help" class="row"><AppIcon name="help" /><span class="row-text row-title">{{ t('more.help') }}</span><AppIcon name="right" class="muted" /></RouterLink>
      <label class="row">
        <AppIcon name="globe" />
        <span class="row-text row-title">{{ t('more.language') }}</span>
        <select class="select lang" :value="locale" @change="setLanguage(($event.target as HTMLSelectElement).value)">
          <option v-for="l in languages" :key="l.code" :value="l.code">{{ l.name }}</option>
        </select>
      </label>
      <a href="/swagger" target="_blank" rel="noopener" class="row"><AppIcon name="code" /><span class="row-text row-title">{{ t('more.api') }}</span></a>
      <button class="row danger" @click="session.logout()"><AppIcon name="logout" /><span class="row-text row-title">{{ t('more.logout') }}</span></button>
    </nav>

    <p class="muted version">{{ session.state.server?.name }} {{ session.state.server?.version }}</p>
  </div>
</template>

<style scoped>
.me {
  display: flex;
  align-items: center;
  gap: 14px;
}

.lang {
  width: auto;
  min-height: 40px;
}

.danger {
  color: var(--error);
}

.version {
  margin: 0;
  text-align: center;
  font-size: 0.875rem;
}
</style>
