import {
  ApplicationConfig,
  LOCALE_ID,
  provideZoneChangeDetection,
  isDevMode,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { provideServiceWorker } from '@angular/service-worker';
import { provideNativeDateAdapter } from '@angular/material/core';

import { registerLocaleData } from '@angular/common';
import localePH from '@angular/common/locales/en-PH';

registerLocaleData(localePH);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    provideNativeDateAdapter(),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),

    { provide: LOCALE_ID, useValue: 'en-PH' },
  ],
};
