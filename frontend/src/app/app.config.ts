import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { MAT_ICON_DEFAULT_OPTIONS } from '@angular/material/icon';
import { authInterceptor, provideAuth } from 'angular-auth-oidc-client';

import { routes } from './app.routes';
import { authConfig } from './core/auth/auth.config';
import { AuthService } from './core/auth/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideAuth(authConfig),
    // authInterceptor attaches the access token to the API (secureRoutes = apiBaseUrl).
    provideHttpClient(withFetch(), withInterceptors([authInterceptor()])),
    // Process the OIDC redirect + restore the session before routes/guards run.
    provideAppInitializer(() => inject(AuthService).init()),
    // The design uses Material Symbols Outlined; make it the default mat-icon font set.
    { provide: MAT_ICON_DEFAULT_OPTIONS, useValue: { fontSet: 'material-symbols-outlined' } },
  ],
};
