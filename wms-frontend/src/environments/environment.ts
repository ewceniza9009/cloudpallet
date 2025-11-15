// ---- File: wms-frontend/src/environments/environment.ts ----
export const environment = {
  production: false,
  apiUrl: 'https://localhost:44335/api',
  hubs: {
    temperature: 'https://localhost:44335/hubs/temperature',
    docks: 'https://localhost:44335/hubs/docks',
    notifications: 'https://localhost:44335/hubs/notifications'
  }
};
