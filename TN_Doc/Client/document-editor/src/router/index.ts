import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import DocumentEditor from '@/views/DocumentEditor.vue'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/edit/:deviceId/:docType/:docId',
    name: 'DocumentEdit',
    component: DocumentEditor,
    props: (route) => ({
      deviceId: route.params.deviceId as string,
      docType: route.params.docType as string,
      docId: Number(route.params.docId)
    }),
    meta: {
      title: 'Редактирование документа'
    }
  },
  {
    path: '/',
    redirect: () => {
      // Если нет параметров, перенаправляем на страницу ошибки
      return { name: 'Error', params: { message: 'Не указаны параметры документа' } }
    }
  },
  {
    path: '/error/:message?',
    name: 'Error',
    component: () => import('@/views/ErrorPage.vue'),
    props: true
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/ErrorPage.vue'),
    props: { message: 'Страница не найдена' }
  }
]

const router = createRouter({
  history: createWebHistory('/document-editor'),
  routes
})

// Навигационный guard для установки заголовка страницы
router.beforeEach((to, from, next) => {
  const title = to.meta.title as string || 'Document Editor'
  document.title = title
  next()
})

export default router
