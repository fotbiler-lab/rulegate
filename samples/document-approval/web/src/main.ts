import { bootstrapApplication } from '@angular/platform-browser';

import { AppComponent } from './app/app.component';
import { createAppConfig } from './app/app.config';
import { loadAppConfiguration } from './app/core/app-settings';

loadAppConfiguration()
  .then((configuration) => bootstrapApplication(AppComponent, createAppConfig(configuration)))
  .catch((error: unknown) => console.error(error));
