import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { User, License, Dictionaries } from '../types/field.types'

export const useDictionaryStore = defineStore('dictionary', () => {
  // State
  const users = ref<User[]>([])
  const licenses = ref<License[]>([])

  // Actions
  const init = (dictionaries: Dictionaries) => {
    if (dictionaries.Users) {
      users.value = dictionaries.Users
    }
    if (dictionaries.Licenses) {
      licenses.value = dictionaries.Licenses
    }
  }

  const getUserById = (id: number): User | undefined => {
    return users.value.find(u => u.Id === id)
  }

  const getLicenseById = (id: number): License | undefined => {
    return licenses.value.find(l => l.Id === id)
  }

  return {
    // State
    users,
    licenses,

    // Actions
    init,
    getUserById,
    getLicenseById
  }
})
