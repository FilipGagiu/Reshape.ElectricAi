import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from '@config/app.config';
import { App } from './app/components/main/app.component';

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
