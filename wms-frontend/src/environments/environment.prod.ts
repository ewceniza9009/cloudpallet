// ---- File: wms-frontend/src/environments/environment.prod.ts ----
export const environment = {
  production: true,
  apiUrl: 'https://api.wms.your-company.com/api',
  hubs: {
    temperature: 'https://api.wms.your-company.com/hubs/temperature',
    docks: 'https://api.wms.your-company.com/hubs/docks',
    notifications: 'https://api.wms.your-company.com/hubs/notifications'
  }
};
