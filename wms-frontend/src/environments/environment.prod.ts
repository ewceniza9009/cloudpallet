// ---- File: wms-frontend/src/environments/environment.prod.ts ----
export const environment = {
  production: true,
  apiUrl: 'https://demo-api.cloudpallet.com/api', // Placeholder for demo
  hubs: {
    temperature: 'https://demo-api.cloudpallet.com/hubs/temperature',
    docks: 'https://demo-api.cloudpallet.com/hubs/docks',
    notifications: 'https://demo-api.cloudpallet.com/hubs/notifications'
  }
};
