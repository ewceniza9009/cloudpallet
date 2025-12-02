// ---- File: wms-frontend/src/environments/environment.prod.ts ----
export const environment = {
  production: true,
  apiUrl: 'https://cloudpallet.onrender.com/api',
  hubs: {
    temperature: 'https://cloudpallet.onrender.com/hubs/temperature',
    docks: 'https://cloudpallet.onrender.com/hubs/docks',
    notifications: 'https://cloudpallet.onrender.com/hubs/notifications'
  }
};
