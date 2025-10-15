import { useFormStore } from '../stores/formStore'
import { useDictionaryStore } from '../stores/dictionaryStore'
import type { FieldConfig } from '../types/field.types'

export function useUserChange() {
  const formStore = useFormStore()
  const dictStore = useDictionaryStore()

  const handleUserChange = (userId: number, field: FieldConfig) => {
    if (!userId) return

    const user = dictStore.getUserById(userId)
    if (!user) return

    // Автозаполнение полей в зависимости от типа поля
    if (field.name === 'Delive_IOF') {
      formStore.updateField('Delive_Post', user.Post || '')
      formStore.updateField('Delive_Factory', user.Factory || '')
      formStore.updateField('Delive_FIO', user.Name || '')

      if (user.LicenseNumber) {
        formStore.updateField('Delive_Lic_Number', user.LicenseNumber)
      }
      if (user.LicenseDate) {
        formStore.updateField('Delive_Lic_Date', user.LicenseDate)
      }
    } else if (field.name === 'Receive_IOF') {
      formStore.updateField('Receive_Post', user.Post || '')
      formStore.updateField('Receive_Factory', user.Factory || '')
      formStore.updateField('Receive_FIO', user.Name || '')

      if (user.LicenseNumber) {
        formStore.updateField('Receive_Lic_Number', user.LicenseNumber)
      }
      if (user.LicenseDate) {
        formStore.updateField('Receive_Lic_Date', user.LicenseDate)
      }
    } else if (field.name === 'Laboratory_IOF') {
      formStore.updateField('Laboratory_Post', user.Post || '')
      formStore.updateField('Laboratory_Factory', user.Factory || '')
      formStore.updateField('Laboratory_FIO', user.Name || '')
    }

    // Универсальное автозаполнение на основе конфигурации поля
    if (field.autoFill && field.autoFill.length > 0) {
      field.autoFill.forEach(targetFieldName => {
        if (targetFieldName.includes('Post')) {
          formStore.updateField(targetFieldName, user.Post || '')
        } else if (targetFieldName.includes('Factory')) {
          formStore.updateField(targetFieldName, user.Factory || '')
        } else if (targetFieldName.includes('FIO') && !targetFieldName.includes('IOF')) {
          formStore.updateField(targetFieldName, user.Name || '')
        } else if (targetFieldName.includes('Lic_Number')) {
          formStore.updateField(targetFieldName, user.LicenseNumber || '')
        } else if (targetFieldName.includes('Lic_Date')) {
          formStore.updateField(targetFieldName, user.LicenseDate || '')
        }
      })
    }
  }

  return {
    handleUserChange
  }
}
