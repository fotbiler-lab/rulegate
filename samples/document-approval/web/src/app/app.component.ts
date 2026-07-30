import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AppLayoutComponent } from './layout/app-layout.component';

@Component({
  selector: 'app-root',
  imports: [AppLayoutComponent, RouterOutlet],
  template: `
    <app-layout>
      <router-outlet />
    </app-layout>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {}
