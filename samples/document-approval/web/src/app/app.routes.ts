import { Routes } from '@angular/router';
import { ruleGateGuard, ruleGateRouteData } from '@fotbiler/rulegate-angular';

import { RuleGateIdentifiers } from './generated/rulegate';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/dashboard.component').then((module) => module.DashboardComponent),
  },
  {
    path: 'documents',
    loadComponent: () =>
      import('./pages/documents.component').then((module) => module.DocumentsComponent),
    canActivate: [ruleGateGuard],
    data: ruleGateRouteData({ permission: RuleGateIdentifiers.permissions.docRead }),
  },
  {
    path: 'approvals',
    loadComponent: () =>
      import('./pages/documents.component').then((module) => module.DocumentsComponent),
    canActivate: [ruleGateGuard],
    data: ruleGateRouteData({ role: RuleGateIdentifiers.roles.keycloakRealmApprover }),
  },
  {
    path: 'access-denied',
    loadComponent: () =>
      import('./pages/access-denied.component').then((module) => module.AccessDeniedComponent),
  },
  { path: '**', redirectTo: '' },
];
