import { NgModule } from '@angular/core';

import { RuleGateLegacyCanDirective } from './rule-gate-legacy-can.directive';
import { RuleGateLegacyDisableDirective } from './rule-gate-legacy-disable.directive';

@NgModule({
  declarations: [RuleGateLegacyCanDirective, RuleGateLegacyDisableDirective],
  exports: [RuleGateLegacyCanDirective, RuleGateLegacyDisableDirective],
})
export class RuleGateLegacyModule {}
