import { Injectable } from '@angular/core';
import {
  RuleGateAuthorizationRequirement,
  RuleGateAuthorizationSnapshot,
  RuleGateAuthorizationStore,
} from '@fotbiler/rulegate-client';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RuleGateLegacyAuthorizationClient {
  private readonly store = new RuleGateAuthorizationStore();
  private readonly stateSubject = new BehaviorSubject<RuleGateAuthorizationSnapshot>(
    this.store.snapshot,
  );

  readonly snapshot$: Observable<RuleGateAuthorizationSnapshot> = this.stateSubject.asObservable();

  get isReady(): boolean {
    return this.store.isReady;
  }

  get snapshot(): RuleGateAuthorizationSnapshot {
    return this.store.snapshot;
  }

  replaceSnapshot(snapshot: RuleGateAuthorizationSnapshot): boolean {
    const accepted = this.store.replaceSnapshot(snapshot);
    this.stateSubject.next(this.store.snapshot);
    return accepted;
  }

  clear(): void {
    this.store.clear();
    this.stateSubject.next(this.store.snapshot);
  }

  hasPermission(permission: string): boolean {
    return this.store.hasPermission(permission);
  }

  hasPolicy(policy: string): boolean {
    return this.store.hasPolicy(policy);
  }

  hasRole(role: string): boolean {
    return this.store.hasRole(role);
  }

  isGranted(requirement: RuleGateAuthorizationRequirement | null | undefined): boolean {
    return this.store.isGranted(requirement);
  }
}
