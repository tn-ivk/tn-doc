import { createRouter, createWebHistory } from 'vue-router';
import DocumentEditor from '@/views/DocumentEditor.vue';
import DocumentActEditor from '@/views/DocumentActEditor.vue';
import DocumentPassportEditor from '@/views/DocumentPassportEditor.vue';
import ErrorPage from '@/views/ErrorPage.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      // Специальный маршрут для редактирования Актов
      path: '/edit/:deviceId/Act/:id',
      name: 'act-editor',
      component: DocumentActEditor,
      props: true
    },
    {
      // Специальный маршрут для редактирования Паспортов качества
      path: '/edit/:deviceId/Passport/:id',
      name: 'passport-editor',
      component: DocumentPassportEditor,
      props: true
    },
    {
      // Общий маршрут для редактирования других документов
      path: '/edit/:deviceId/:docType/:id',
      name: 'editor',
      component: DocumentEditor,
      props: true
    },
    {
      path: '/error',
      name: 'error',
      component: ErrorPage
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/error?message=Страница не найдена'
    }
  ]
});

export default router;
